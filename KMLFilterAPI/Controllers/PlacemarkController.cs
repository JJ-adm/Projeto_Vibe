using KMLFilterAPI.Services;
using Microsoft.AspNetCore.Mvc;
using KMLFilterAPI.Models;

[ApiController]
[Route("api/[controller]")]
public class PlacemarkController : ControllerBase
{
    private readonly KmlService _kmlService;

    public PlacemarkController()
    {
        _kmlService = new KmlService("Arquivos/DIRECIONADORES1.kml");
    }

    [HttpGet]
    public IActionResult GetPlacemarks(
        [FromQuery] string cliente,
        [FromQuery] string situacao,
        [FromQuery] string bairro,
        [FromQuery] string referencia,
        [FromQuery] string ruaCruzamento)
    {
        try
        {
            var filters = new Filters
            {
                Cliente = cliente,
                Situacao = situacao,
                Bairro = bairro,
                Referencia = referencia,
                RuaCruzamento = ruaCruzamento
            };

            _kmlService.ValidateFilters(filters);
            var placemarks = _kmlService.GetFilteredPlacemarks(cliente, situacao, bairro, referencia, ruaCruzamento);
            return Ok(placemarks);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("filters")]
    public IActionResult GetFilters()
    {
        try
        {
            var filters = _kmlService.GetAvailableFilters();
            return Ok(filters);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("export")]
    public IActionResult ExportPlacemarks([FromBody] Filters filters)
    {
        try
        {
            _kmlService.ValidateFilters(filters);

            var kmlFilePath = _kmlService.ExportFilteredPlacemarks(filters);
            var fileBytes = System.IO.File.ReadAllBytes(kmlFilePath);
            var fileName = "filtered.kml";

            return File(fileBytes, "application/vnd.google-earth.kml+xml", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}