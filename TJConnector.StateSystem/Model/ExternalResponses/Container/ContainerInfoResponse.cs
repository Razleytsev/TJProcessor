namespace TJConnector.StateSystem.Model.ExternalResponses.Container;

public class ContainerInfoResponse
{
    public int? statusCode { get; set; }
    public string? message { get; set; }
    public string? code { get; set; }
    public int authenticity { get; set; }
    public string? batchNumber { get; set; }
    public DateTime? createdAt { get; set; }
    public object? dueDate { get; set; }
    public string? emissionRequestUuid { get; set; }
    public Guid? id { get; set; }
    public Guid? lastContainerOperationUuid { get; set; }
    public Guid? lastOperationUuid { get; set; }
    public string? locationName { get; set; }
    public Guid? locationUuid { get; set; }
    public string? ownerEmployerIdNumber { get; set; }
    public string? ownerName { get; set; }
    public string? ownerTaxIdNumber { get; set; }
    public string? ownerUuid { get; set; }
    public string? parentCode { get; set; }
    public Guid? productUuid { get; set; }
    public string? productionDate { get; set; }
    public string? providedAt { get; set; }
    public int? reason { get; set; }
    public string serialNumber { get; set; } = string.Empty;
    public int status { get; set; }
    public int type { get; set; }
    public string? verificationCode { get; set; }
    public DateTime? version { get; set; }
}

public class CustomResultCollection<T>
{
    public List<T>? Content { get; set; }

    public bool Success { get; set; }
    public string? Message { get; set; }
}


public class CustomResult<T>
{
    public T? Content { get; set; }

    public bool Success { get; set; }
    public string? Message { get; set; }
}