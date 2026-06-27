/**
 * Política de senha do cliente — espelha o PasswordPolicy do backend
 * (8+ caracteres com maiúscula, minúscula e número). Serve para feedback
 * imediato; a validação que vale é a do backend.
 */
export const PASSWORD_MIN_LENGTH = 8;

export function avaliarSenha(senha) {
  const s = senha || '';
  const requisitos = [
    { label: `Mínimo de ${PASSWORD_MIN_LENGTH} caracteres`, ok: s.length >= PASSWORD_MIN_LENGTH },
    { label: 'Uma letra maiúscula', ok: /[A-Z]/.test(s) },
    { label: 'Uma letra minúscula', ok: /[a-z]/.test(s) },
    { label: 'Um número', ok: /\d/.test(s) },
  ];
  return { valida: requisitos.every((r) => r.ok), requisitos };
}

/** Mensagem do primeiro requisito não atendido (igual à do backend), ou null. */
export function primeiroErroSenha(senha) {
  const s = senha || '';
  if (s.length < PASSWORD_MIN_LENGTH) {
    return `A senha deve ter ao menos ${PASSWORD_MIN_LENGTH} caracteres.`;
  }
  if (!/[A-Z]/.test(s) || !/[a-z]/.test(s) || !/\d/.test(s)) {
    return 'A senha deve conter letra maiúscula, letra minúscula e número.';
  }
  return null;
}
