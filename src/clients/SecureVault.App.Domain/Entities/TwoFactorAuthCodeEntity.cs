using Realms;
using SecureVault.App.Domain.Enums;

namespace SecureVault.App.Domain.Entities;

public partial class TwoFactorAuthCodeEntity : IRealmObject, ISynchronizableEntity
{
    [PrimaryKey]
    public Guid Id { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int TypeRaw { get; set; }
    public int AlgorithmRaw { get; set; }

    [Ignored]
    public OtpType Type
    {
        get => (OtpType)TypeRaw;
        set => TypeRaw = (int)value;
    }

    [Ignored]
    public OtpAlgorithm Algorithm
    {
        get => (OtpAlgorithm)AlgorithmRaw;
        set => AlgorithmRaw = (int)value;
    }
    public int Digits { get; set; }
    public int Period { get; set; }
    public long Counter { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }

    public TwoFactorAuthCodeEntity()
    {
        Id = Guid.NewGuid();
        Version = 1;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        IsDeleted = false;
        IsSynced = false;
    }



    public void MarkAsDeleted()
    {
        IsDeleted = true;
        IsSynced = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsSynced()
    {
        IsSynced = true;
    }
}
