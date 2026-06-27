using WhatsFlow.Application.DTOs;

namespace WhatsFlow.Application.Interfaces;

/// <summary>
/// Onboarding self-service: cadastro público de uma nova organização (provisiona tenant
/// inativo + admin + trial + consentimento) e confirmação por e-mail que ativa o acesso.
/// </summary>
public interface ISignupService
{
    Task<SignupResultDto> SignupAsync(SignupDto dto);
    Task<ConfirmacaoEmailResultDto> ConfirmarAsync(string token);
}
