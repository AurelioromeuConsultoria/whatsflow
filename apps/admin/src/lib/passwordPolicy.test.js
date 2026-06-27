import { avaliarSenha, primeiroErroSenha } from '@/lib/passwordPolicy';

describe('passwordPolicy', () => {
  it('aceita senha forte', () => {
    expect(avaliarSenha('Senha123').valida).toBe(true);
    expect(primeiroErroSenha('Senha123')).toBeNull();
  });

  it('reprova senha curta com mensagem de comprimento', () => {
    expect(avaliarSenha('Ab1').valida).toBe(false);
    expect(primeiroErroSenha('Ab1')).toContain('8 caracteres');
  });

  it('reprova senha sem complexidade', () => {
    expect(primeiroErroSenha('semmaiuscula1')).toContain('maiúscula');
    expect(primeiroErroSenha('SENHA12345')).toContain('maiúscula'); // sem minúscula
    expect(primeiroErroSenha('SenhaSemNumero')).toContain('maiúscula'); // sem número
  });

  it('lista os requisitos com o estado de cada um', () => {
    const { requisitos } = avaliarSenha('abc');
    expect(requisitos).toHaveLength(4);
    expect(requisitos.find((r) => r.label.includes('minúscula')).ok).toBe(true);
    expect(requisitos.find((r) => r.label.includes('número')).ok).toBe(false);
  });
});
