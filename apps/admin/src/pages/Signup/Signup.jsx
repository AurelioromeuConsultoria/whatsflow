import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { MailCheck } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { signupApi } from '@/lib/api';
import { getApiErrorMessage } from '@/lib/apiError';
import { primeiroErroSenha } from '@/lib/passwordPolicy';
import { PasswordRequirements } from '@/components/PasswordRequirements';
import { toast } from 'sonner';
import { Toaster } from '@/components/ui/sonner';

const TERMOS_VERSAO = 'v1';

const PLANOS = [
  { slug: 'essencial', nome: 'Essencial' },
  { slug: 'organizacao', nome: 'Organização' },
  { slug: 'crescimento', nome: 'Crescimento' },
];

export default function Signup() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const planoInicial = PLANOS.some((p) => p.slug === searchParams.get('plano'))
    ? searchParams.get('plano')
    : 'organizacao';

  const [form, setForm] = useState({
    nomeIgreja: '',
    adminNome: '',
    email: '',
    senha: '',
    telefone: '',
    planoSlug: planoInicial,
    aceiteTermos: false,
  });
  const [loading, setLoading] = useState(false);
  const [resultado, setResultado] = useState(null);

  const set = (campo, valor) => setForm((f) => ({ ...f, [campo]: valor }));

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!form.nomeIgreja.trim() || !form.adminNome.trim() || !form.email.trim() || !form.senha) {
      toast.error('Preencha todos os campos obrigatórios.');
      return;
    }
    const erroSenha = primeiroErroSenha(form.senha);
    if (erroSenha) {
      toast.error(erroSenha);
      return;
    }
    if (!form.aceiteTermos) {
      toast.error('É necessário aceitar os Termos de Uso e a Política de Privacidade.');
      return;
    }

    setLoading(true);
    try {
      const resp = await signupApi.signup({
        nomeIgreja: form.nomeIgreja.trim(),
        adminNome: form.adminNome.trim(),
        email: form.email.trim(),
        senha: form.senha,
        telefone: form.telefone.trim() || null,
        planoSlug: form.planoSlug,
        aceiteTermosVersao: TERMOS_VERSAO,
      });
      setResultado(resp.data);
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Não foi possível concluir o cadastro.'));
    } finally {
      setLoading(false);
    }
  };

  if (resultado) {
    return (
      <>
        <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6 py-10">
          <div className="w-full max-w-md rounded-xl bg-white p-10 text-center shadow-sm">
            <MailCheck className="mx-auto mb-4 size-12 text-[#2563eb]" />
            <h1 className="text-2xl font-bold text-[#1e4f82]">Verifique seu e-mail</h1>
            <p className="mt-3 text-slate-500">
              Enviamos um link de confirmação para <strong>{resultado.email}</strong>. Confirme para ativar a sua organização.
            </p>
            {resultado.linkConfirmacao && (
              <p className="mt-5 rounded-md bg-amber-50 p-3 text-sm text-amber-800">
                Ambiente de testes: <a className="font-semibold underline" href={resultado.linkConfirmacao}>confirmar agora</a>
              </p>
            )}
            <Button className="mt-6 w-full" onClick={() => navigate('/login')}>Ir para o login</Button>
          </div>
        </div>
        <Toaster />
      </>
    );
  }

  return (
    <>
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6 py-10">
        <div className="w-full max-w-md rounded-xl bg-white p-8 shadow-sm">
          <div className="mb-6 flex items-center justify-center gap-3">
            <span className="text-3xl" aria-hidden="true">💬</span>
            <span className="text-2xl font-bold tracking-tight text-slate-950">WhatsFlow</span>
          </div>
          <h1 className="text-2xl font-bold text-[#1e4f82]">Comece grátis</h1>
          <p className="mt-2 mb-6 text-sm text-slate-500">Crie a conta do seu workspace. Sem cartão para iniciar o período de teste.</p>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="nomeIgreja">Nome da igreja *</Label>
              <Input id="nomeIgreja" value={form.nomeIgreja} onChange={(e) => set('nomeIgreja', e.target.value)} maxLength={150} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="adminNome">Seu nome *</Label>
              <Input id="adminNome" value={form.adminNome} onChange={(e) => set('adminNome', e.target.value)} maxLength={150} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="email">E-mail *</Label>
              <Input id="email" type="email" value={form.email} onChange={(e) => set('email', e.target.value)} maxLength={100} autoComplete="email" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="senha">Senha * <span className="font-normal text-slate-400">(mín. 8 caracteres)</span></Label>
              <Input id="senha" type="password" value={form.senha} onChange={(e) => set('senha', e.target.value)} autoComplete="new-password" />
              <PasswordRequirements senha={form.senha} />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="plano">Plano</Label>
              <Select value={form.planoSlug} onValueChange={(v) => set('planoSlug', v)}>
                <SelectTrigger id="plano"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {PLANOS.map((p) => <SelectItem key={p.slug} value={p.slug}>{p.nome}</SelectItem>)}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-400">Você pode trocar de plano a qualquer momento durante o teste.</p>
            </div>

            <label htmlFor="aceiteTermos" className="flex items-start gap-2 text-sm text-slate-600">
              <input id="aceiteTermos" type="checkbox" className="mt-1" checked={form.aceiteTermos} onChange={(e) => set('aceiteTermos', e.target.checked)} />
              <span>
                Li e aceito os <a href="/termos-de-uso.html" target="_blank" rel="noopener" className="text-[#2563eb] underline">Termos de Uso</a> e a{' '}
                <a href="/politica-de-privacidade.html" target="_blank" rel="noopener" className="text-[#2563eb] underline">Política de Privacidade</a>. *
              </span>
            </label>

            <Button type="submit" className="h-12 w-full bg-[#2563eb] text-base font-bold hover:bg-[#1d4ed8]" disabled={loading}>
              {loading ? 'Criando...' : 'Criar conta grátis'}
            </Button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            Já tem conta? <Link to="/login" className="font-semibold text-[#2563eb]">Entrar</Link>
          </p>
        </div>
      </div>
      <Toaster />
    </>
  );
}
