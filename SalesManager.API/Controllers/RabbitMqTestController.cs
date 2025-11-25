using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SalesManager.API.Application.Events;

namespace SalesManager.API.Controllers
{
    [ApiController]
    [Route("api/test-rabbitmq")]
    public class RabbitMqTestController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public RabbitMqTestController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost]
        public async Task<IActionResult> PublicarMensagemTeste()
        {
            var evento = new TesteMensageriaEvent
            {
                Mensagem = "Olá RabbitMQ! Testando conexão do SalesManager.",
                DataEnvio = DateTime.UtcNow
            };

            try
            {
                await _publishEndpoint.Publish(evento);

                return Ok(new { Status = "Sucesso", Detalhe = "Mensagem enviada para o RabbitMQ (verifique a aba Exchanges)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "Erro", Erro = ex.Message });
            }
        }
    }
}