namespace ArtemisBankingPro.Helpers;

public static class TransactionDisplayText
{
    public static string LocalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Transacción";

        return description switch
        {
            "Initial deposit for secondary account opening" => "Depósito inicial - apertura de cuenta secundaria",
            "Closure of secondary account - Balance transferred to primary" => "Cierre de cuenta secundaria - saldo transferido a la principal",
            _ when description.StartsWith("Loan disbursement ", StringComparison.OrdinalIgnoreCase)
                => $"Desembolso de préstamo {description["Loan disbursement ".Length..]}",
            _ when description.StartsWith("Hermes Pay settlement - ", StringComparison.OrdinalIgnoreCase)
                => $"Liquidación de Hermes Pay - {description["Hermes Pay settlement - ".Length..]}",
            _ => description
        };
    }
}
