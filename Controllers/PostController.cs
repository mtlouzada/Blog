using Blog.Data;
using Blog.Models;
using Blog.ViewModels;
using Blog.ViewModels.Posts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers;

[ApiController]
public class PostController : ControllerBase
{
    [HttpGet("v1/posts")]
    public async Task<IActionResult> GetAsync(
        [FromServices] BlogDataContext context,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25)
    {
        try
        {
            var count = await context.Posts.AsNoTracking().CountAsync();
            var posts = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Select(x => new ListPostsViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Slug = x.Slug,
                    LastUpdateDate = x.LastUpdateDate,
                    Category = x.Category.Name,
                    Author = $"{x.Author.Name} ({x.Author.Email})"
                })
                // Ordenar antes de paginar: invertido, cada página era ordenada
                // isoladamente e a "primeira página" não trazia os posts mais recentes.
                .OrderByDescending(x => x.LastUpdateDate)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new ResultViewModel<dynamic>(new
            {
                total = count,
                page,
                pageSize,
                posts
            }));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<List<Category>>("05X04 - Falha interna no servidor"));
        }
    }

    [HttpGet("v1/posts/{id:int}")]
    public async Task<IActionResult> DetailsAsync(
        [FromServices] BlogDataContext context,
        [FromRoute] int id)
    {
        try
        {
            var post = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Author)
                .ThenInclude(x => x.Roles)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (post == null)
                return NotFound(new ResultViewModel<Post>("Conteúdo não encontrado"));

            return Ok(new ResultViewModel<Post>(post));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ResultViewModel<List<Category>>("05X04 - Falha interna no servidor"));
        }
    }

    [HttpGet("v1/posts/category/{category}")]
    public async Task<IActionResult> GetByCategoryAsync(
        [FromRoute] string category,
        [FromServices] BlogDataContext context,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25)
    {
        try
        {
            // O total precisa refletir o mesmo filtro da consulta: contar todos os
            // posts do blog faria o cliente paginar sobre um número inexistente.
            var count = await context
                .Posts
                .AsNoTracking()
                .CountAsync(x => x.Category.Slug == category);

            var posts = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Author)
                .Include(x => x.Category)
                .Where(x => x.Category.Slug == category)
                .Select(x => new ListPostsViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    Slug = x.Slug,
                    LastUpdateDate = x.LastUpdateDate,
                    Category = x.Category.Name,
                    Author = $"{x.Author.Name} ({x.Author.Email})"
                })
                .OrderByDescending(x => x.LastUpdateDate)
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new ResultViewModel<dynamic>(new
            {
                total = count,
                page,
                pageSize,
                posts
            }));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<List<Category>>("05X04 - Falha interna no servidor"));
        }
    }
}