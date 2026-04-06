using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly IContentService _contentService;

    public ContentController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _contentService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _contentService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateContentRequest request, CancellationToken cancellationToken)
    {
        var item = await _contentService.CreateAsync(request, cancellationToken);
        if (item is null)
        {
            return BadRequest("Не удалось создать контент. Проверьте CategoryId и TagIds.");
        }

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContentRequest request, CancellationToken cancellationToken)
    {
        var item = await _contentService.UpdateAsync(id, request, cancellationToken);
        return item is null ? BadRequest("Не удалось обновить контент.") : Ok(item);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _contentService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

