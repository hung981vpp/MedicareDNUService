using MedicalAPI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAPI.Controllers;

[Route("api/v1/medical/medicines")]
public sealed class MedicinesController(IMedicalRecordService service) : MedicalControllerBase
{
    [HttpGet]
    [EndpointSummary("Lấy danh mục thuốc từ Pharmacy & Billing Service")]
    public IActionResult GetCatalog(
        [FromQuery] string? name,
        [FromQuery] string? activeIngredient,
        [FromQuery] string? status)
        => ToActionResult(service.GetMedicineCatalog(name, activeIngredient, status));
}
