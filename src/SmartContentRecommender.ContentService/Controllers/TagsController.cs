using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _tagService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateNameRequest request, CancellationToken cancellationToken)
    {
        var item = await _tagService.CreateAsync(request, cancellationToken);
        return item is null ? BadRequest() : Ok(item);
    }
}

