using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Content.Interfaces;
using SmartContentRecommender.Application.Content.Models;

namespace SmartContentRecommender.ContentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _categoryService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateNameRequest request, CancellationToken cancellationToken)
    {
        var item = await _categoryService.CreateAsync(request, cancellationToken);
        return item is null ? BadRequest() : Ok(item);
    }
}

