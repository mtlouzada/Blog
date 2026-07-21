# Post para o LinkedIn

## Versão principal

> As duas primeiras linhas são o que aparece antes do "ver mais" — é o que decide se
> alguém abre o post.

---

Escrevi 28 testes de integração para uma API .NET que eu já considerava pronta.

Eles acharam 6 bugs. Um deles quebrava 100% dos cadastros de usuário.

O mais grave era invisível. Em algum momento eu editei o mapeamento do Entity Framework para tornar dois campos opcionais — e nunca gerei a migration correspondente. O banco continuou com as colunas NOT NULL.

Resultado: todo INSERT de usuário falhava. E como o controller tinha um `catch (DbUpdateException)` genérico que assumia "e-mail duplicado", a API respondia:

"05X99 - Este E-mail já está cadastrado"

No primeiro cadastro. Sempre. Mensagem plausível, log limpo, nenhuma pista.

Um teste unitário do controller passaria tranquilo. Em teste unitário não existe schema, não existe migration, não existe banco.

Os outros cinco:

• A paginação de posts fazia Skip/Take ANTES do OrderBy — a "primeira página" devolvia os posts mais antigos
• O total da listagem por categoria contava todos os posts do blog, não os da categoria
• O cache de categorias nunca era invalidado na escrita: categoria recém-criada sumia da listagem por 1 hora
• O hash de senha de todo autor ia junto no JSON de GET /v1/posts/{id}
• O upload de imagem gravava em caminho relativo ao processo e salvava a URL fixa "localhost:0000"

Três decisões que fizeram a diferença:

1️⃣ Banco real, não InMemory. O provider InMemory do EF Core não tem schema, índice único nem constraint — ele aceita tudo. Quatro dos seis bugs passariam batido nele.

2️⃣ Os testes descrevem o comportamento esperado, não o atual. Se eu tivesse escrito "espero 500" para documentar o que a API fazia hoje, teria uma suíte verde carimbando defeitos.

3️⃣ Só duas fronteiras foram substituídas: o SMTP e o diretório de arquivos. Todo o resto é o código que vai para produção. Quanto mais você mocka, mais o teste verifica a sua imaginação sobre o sistema — e não o sistema.

A suíte inteira roda em 7 segundos: sobe a API em memória, aplica as migrations num banco descartável e limpa os dados entre os testes.

Teste de integração não é aquele teste "caro e lento" que fica para depois. É o único que percebe quando o seu código e o seu banco discordam.

Código no repositório: [link]

#dotnet #csharp #testes #qualidadedesoftware #backend

---

## Versão curta (para quem prefere post enxuto)

Escrevi 28 testes de integração para uma API .NET que eu achava que estava pronta.

Eles acharam 6 bugs. O pior: eu tinha mudado o mapeamento do Entity Framework sem gerar a migration. O banco seguiu com as colunas NOT NULL, todo INSERT de usuário falhava — e um `catch (DbUpdateException)` genérico traduzia a falha para "Este E-mail já está cadastrado".

Ou seja: nenhum cadastro funcionava, e a mensagem de erro era plausível demais para levantar suspeita.

Teste unitário do controller passaria. Em teste unitário não existe schema.

A lição que fica: use banco de verdade na suíte de integração. O provider InMemory não tem índice único, nem constraint, nem tipo de coluna — ele aceita tudo, e quatro dos seis bugs teriam passado batido.

7 segundos de suíte. 6 defeitos a menos em produção.

#dotnet #csharp #testes

---

## Sugestões de publicação

**Imagem:** um print do terminal com `Passed! - Failed: 0, Passed: 28` funciona bem.
Melhor ainda: um print do JSON de resposta com o `passwordHash` aparecendo — a
evidência do vazamento é mais concreta que qualquer texto.

**Primeiro comentário:** deixe o link do repositório no primeiro comentário em vez do
corpo do post (links no corpo costumam reduzir o alcance).

**Se quiser puxar conversa,** termine com uma pergunta genuína, do tipo:
"Qual foi o bug mais constrangedor que um teste seu já encontrou?"

**O que não fazer:** não transforme em tutorial. O post funciona porque conta uma
falha real e específica. Detalhe técnico demais mata o alcance; o guia completo fica
em `docs/testes-de-integracao.md`, que é para onde você leva a conversa numa
entrevista.
