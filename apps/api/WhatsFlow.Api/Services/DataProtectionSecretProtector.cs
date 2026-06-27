using Microsoft.AspNetCore.DataProtection;
using WhatsFlow.Application.Interfaces;

namespace WhatsFlow.API.Services;

public class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("WhatsFlow.Giving.Secrets.v1");
    }

    public string Protect(string value)
    {
        return _protector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        return _protector.Unprotect(protectedValue);
    }
}
