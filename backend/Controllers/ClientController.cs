using backend.DTOs.Client;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly IAgentService _agentService;

    public ClientController(IClientService clientService, IAgentService agentService)
    {
        _clientService = clientService;
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDTO>>> GetAllClients()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var clients = await _clientService.GetClientsByAgentAsync(agent);
        var clientDTOs = _clientService.MapToDTOList(clients);
        return Ok(clientDTOs);
    }

    [HttpPost]
    public async Task<ActionResult<ClientDTO>> CreateClient([FromBody] CreateClientRequest request)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var agent = await _agentService.GetAgentByIdAsync(userId);
        var client = await _clientService.CreateClientAsync(request, agent, agent.User);
        var clientDTO = _clientService.MapToDTO(client);
        return CreatedAtAction(nameof(CreateClient), clientDTO);
    }
}