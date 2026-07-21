# Testes de integração da Blog API

Suíte que sobe a aplicação inteira em memória e conversa com ela por HTTP, contra um
SQL Server de verdade. **28 testes, ~7 segundos.**

O que é exercitado a cada teste: roteamento, model binding, validação, filtros,
autenticação JWT, cache, Entity Framework, o schema real do banco e a serialização
da resposta. Ou seja: quase tudo que existe entre a requisição e a linha do SQL.

---

## Como rodar

```bash
dotnet test tests/Blog.IntegrationTests/Blog.IntegrationTests.csproj
```

Não precisa configurar nada: por padrão a suíte usa o **LocalDB** que já vem com o
SQL Server Express / Visual Studio no Windows.

Para apontar para outro servidor (é o que o CI faz, com um container do SQL Server):

```bash
export BLOG_TESTS_SQLSERVER="Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=true;"
```

A cada execução a suíte cria um banco novo (`BlogTests_<guid>`), aplica as migrations
e o derruba no final. Nenhum banco de desenvolvimento é tocado.

---

## Estrutura

```
tests/Blog.IntegrationTests/
├── Infrastructure/
│   ├── BlogApiFactory.cs          # sobe a aplicação e troca só as fronteiras externas
│   ├── SqlServerTestDatabase.cs   # banco descartável por execução + reset entre testes
│   ├── IntegrationTestBase.cs     # cliente HTTP, limpeza e atalhos de cenário
│   ├── FakeEmailService.cs        # dublê observável no lugar do SMTP
│   ├── TestData.cs                # construtores de cenário
│   └── ApiContracts.cs            # espelhos do contrato HTTP
├── AccountEndpointsTests.cs       # cadastro, hash de senha, login, JWT
├── AuthorizationTests.cs          # pipeline de autenticação e upload autenticado
├── CategoryEndpointsTests.cs      # CRUD, validação, índice único, cache
├── PostEndpointsTests.cs          # paginação, ordenação, filtro e vazamento de dados
└── HomeEndpointTests.cs           # fumaça
```

---

## Decisões e o porquê

### Banco de dados real, não InMemory

O provider InMemory do EF Core não tem schema, não tem tipo de coluna, não tem índice
único e não tem constraint. Ele aceita tudo. Metade dos defeitos que esta suíte
encontrou (índice único, coluna NOT NULL, precisão de `SMALLDATETIME`, tradução da
consulta para SQL) seriam **invisíveis** nele — e o teste passaria dando uma falsa
sensação de cobertura.

A própria documentação da Microsoft recomenda não usar InMemory para testar
comportamento de banco relacional.

O custo é real: precisa de um SQL Server disponível. O preço se paga porque o motor
usado no teste é o mesmo de produção.

### Um banco por execução, limpeza entre os testes

Cada execução cria um database exclusivo — dá para rodar a suíte em paralelo com
outra branch, ou com o CI, sem interferência. Entre um teste e outro, o
[Respawn](https://github.com/jbogard/Respawn) apaga os dados respeitando a ordem das
foreign keys e reinicia os contadores de identity.

A limpeza acontece **antes** de cada teste, e não depois: quando um teste quebra, os
dados dele continuam no banco para você inspecionar.

Alternativa que evitei: transação com rollback por teste. É mais rápida, mas impede
testar qualquer coisa que gerencie transação sozinha e não valida o commit de verdade.

### Só duas fronteiras são substituídas

O SMTP (não é responsabilidade nossa, e nenhum teste deve depender da disponibilidade
de um provedor externo) e o diretório de arquivos estáticos (para não sujar o
repositório com imagens de teste). Todo o resto — inclusive o banco — é o código que
vai para produção.

Quanto mais coisa você substitui por mock, mais o teste passa a verificar a sua
imaginação sobre o sistema em vez do sistema.

### O JWT é obtido pelo endpoint real

Seria mais rápido gerar o token direto pelo `TokenService`. Mas aí o teste deixaria de
exercitar justamente o que costuma quebrar: emissão, assinatura e validação
combinando entre si (chave, algoritmo, claims, esquema Bearer no middleware).

### Os testes não reaproveitam os ViewModels de produção

`ApiContracts.cs` tem espelhos do contrato HTTP. Se alguém renomear uma propriedade
do ViewModel, o teste **tem que quebrar** — é uma quebra de contrato para quem
consome a API. Reaproveitando o tipo, a mudança passaria despercebida.

### Estado compartilhado não é só o banco

O `IMemoryCache` é singleton e sobrevive entre os testes. O reset da suíte invalida o
cache também; sem isso, um teste que cria categorias envenena o seguinte.

### Sem paralelismo entre as classes de teste

Todas as classes estão na mesma xUnit collection, então rodam em sequência
compartilhando uma única instância da aplicação. Subir o host uma vez só é o que
mantém a suíte em segundos. Paralelizar exigiria um banco por classe — complexidade
que só se justifica quando a suíte começa a incomodar no tempo.

---

## O que esta suíte encontrou

Os testes foram escritos descrevendo o comportamento **esperado** de cada endpoint —
não o comportamento atual. Foi assim que apareceram seis defeitos reais, todos
corrigidos neste commit:

| # | Defeito | Efeito em produção |
|---|---|---|
| 1 | Migration cria `User.Bio` e `User.Image` como NOT NULL, mas o mapeamento diz opcional | **Nenhum cadastro de usuário funcionava** num banco criado pelas migrations |
| 2 | `catch (DbUpdateException)` genérico no cadastro | Qualquer falha de escrita virava "Este E-mail já está cadastrado", escondendo a causa real (inclusive o defeito nº 1) |
| 3 | Cache de categorias nunca invalidado na escrita | Categoria criada sumia da listagem por até 1 hora |
| 4 | `Skip/Take` aplicados **antes** do `OrderByDescending` | A primeira página trazia os posts mais **antigos** |
| 5 | `total` da listagem por categoria contava todos os posts do blog | O cliente paginava sobre um número que não existe |
| 6 | Entidade `User` serializada direto na resposta | O **hash de senha** de todo autor ia no JSON de `GET /v1/posts/{id}` |

Bônus corrigidos junto: o upload de imagem gravava em caminho relativo ao diretório de
trabalho do processo (só funcionava sob `dotnet run`) e salvava a URL fixa
`https://localhost:0000/images/...`; e categoria com slug repetido devolvia 500 em vez
de 409.

O nº 1 merece um parágrafo. O `UserMap` foi editado depois que a migration foi gerada,
e nenhuma migration nova foi criada. O banco ficou com `Bio` NOT NULL; o INSERT do
cadastro falhava; o `catch` genérico traduzia a falha para "e-mail já cadastrado". O
usuário via uma mensagem plausível, os logs não mostravam nada e o teste unitário do
controller passaria — porque em teste unitário não existe schema.

---

## Perguntas de entrevista

**Qual a diferença entre teste unitário e de integração aqui?**
O unitário verifica uma unidade isolada, com as dependências substituídas — é rápido e
aponta o erro com precisão, mas não sabe nada sobre como as peças se encaixam. O de
integração verifica o encaixe: HTTP, serialização, EF, schema, migrations,
autenticação. Os seis defeitos acima estão todos *entre* as unidades; nenhum apareceria
em teste unitário de controller.

**Então dá para largar os unitários?**
Não. A suíte de integração é ordens de grandeza mais lenta e diz "quebrou em algum
lugar", não "quebrou nesta linha". Regra prática: unitário para lógica com muitos
ramos (regra de negócio, cálculo, validação), integração para o caminho que atravessa
as fronteiras. Aqui a pirâmide está deliberadamente invertida porque a aplicação quase
não tem lógica própria — ela é cola entre HTTP e banco. Testar cola exige integração.

**Por que não usar o InMemory do EF Core?**
Ver a seção acima: ele não tem schema, índice único, tipo de coluna nem constraint.
Passaria em quatro dos seis defeitos encontrados.

**Como você garante que os testes não interferem entre si?**
Banco exclusivo por execução, Respawn entre os testes, cache invalidado no reset e
execução sequencial dentro de uma única collection.

**Como isso roda no CI?**
`.github/workflows/ci.yml` sobe um container do SQL Server como service container e
aponta a variável `BLOG_TESTS_SQLSERVER` para ele. O mesmo código de teste roda no
LocalDB local e no container do CI, sem `if` nenhum.

**E se não desse para ter um SQL Server no CI?**
Aí eu usaria [Testcontainers](https://dotnet.testcontainers.org/), que sobe o
container programaticamente a partir do próprio teste. Não precisei aqui porque o
GitHub Actions já oferece service containers e a máquina de desenvolvimento tem
LocalDB — a costura da connection string deixa os dois caminhos abertos.

**Qual foi a parte mais difícil?**
A aplicação carrega configuração para uma classe **estática** (`Configuration.Load`)
durante os top-level statements do `Program`, antes de qualquer ponto de extensão do
`WebApplicationFactory` rodar. Nenhum `ConfigureServices` chega a tempo de trocar a
connection string. A saída foi variável de ambiente, que a configuração padrão lê na
construção do builder. O ponto de fundo é que **configuração estática é hostil a
teste** — o certo seria `IOptions<T>` injetado; a variável de ambiente é o contorno
enquanto isso não muda.

**O que você não testou aqui, e por quê?**
O envio real de e-mail (fronteira de terceiro, verificado por contrato via dublê),
HTTPS/certificados (responsabilidade da hospedagem) e performance/carga (é outro tipo
de teste, com outra ferramenta). Também não testei o `HomeController` além do
smoke — ele só devolve o nome do ambiente.

**Como você decidiu o que cada teste afirma?**
Cada teste afirma o comportamento observável pelo cliente da API — status code, corpo
da resposta, e o efeito colateral que importa (linha gravada no banco, arquivo escrito,
e-mail enfileirado). Nenhum teste afirma detalhe interno de implementação: é isso que
permite refatorar o controller sem reescrever a suíte.
