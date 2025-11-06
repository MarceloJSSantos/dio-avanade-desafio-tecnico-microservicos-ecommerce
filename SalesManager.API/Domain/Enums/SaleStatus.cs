namespace SalesManager.API.Domain.Enums
{
    public enum SaleStatus
    {
        // ATENÇÃO: A ordem dos itens são usadas na Regra de Negócio
        PendingPayment,     // Venda criada, aguardando pagamento
        Paid,               // Pagamento confirmado, aguardando envio
        Shipped,            // Produto enviado
        Completed,          // Venda concluída
        Cancelled           // Venda cancelada
    }
}