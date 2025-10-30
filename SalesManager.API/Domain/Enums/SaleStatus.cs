using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Domain.Enums
{
    public enum SaleStatus
    {
        PendingPayment, // Venda criada, aguardando pagamento
        Paid,             // Pagamento confirmado, aguardando envio
        Shipped,          // Produto enviado
        Completed,        // Venda concluída
        Cancelled         // Venda cancelada
    }
}