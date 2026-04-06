using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.ContentService.Controllers;

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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _contentService.GetAllAsync(cancellationToken));

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
        return item is null ? BadRequest() : CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }
}

