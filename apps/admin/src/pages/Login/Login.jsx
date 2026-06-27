import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Eye,
  EyeOff,
  Lock,
  Mail,
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '@/context/AuthContext';
import { toast } from 'sonner';
import { Toaster } from '@/components/ui/sonner';
import { useTranslation } from 'react-i18next';

export default function Login() {
  const navigate = useNavigate();
  const { login, isAuthenticated } = useAuth();
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    email: '',
    senha: '',
  });
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  useEffect(() => {
    // Se já estiver autenticado, redirecionar
    if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!formData.email || !formData.senha) {
      toast.error(t('login.fillAllFields'));
      return;
    }

    setLoading(true);
    const result = await login(formData.email, formData.senha);
    setLoading(false);

    if (result.success) {
      toast.success(t('login.success'));
      navigate('/');
    } else {
      const errorMessage = result.message || t('login.invalidCredentials');
      toast.error(errorMessage);
      setFormData((prev) => ({ ...prev, senha: '' }));
    }
  };

  return (
    <>
      <div className="grid min-h-screen overflow-hidden bg-white text-[#0f172a] lg:grid-cols-[42%_58%]">
        <aside className="relative hidden min-h-screen overflow-hidden bg-[#07172a] text-white lg:flex lg:flex-col lg:items-center">
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_92%_8%,rgba(6,182,212,0.18),transparent_28%),radial-gradient(circle_at_0%_100%,rgba(124,58,237,0.18),transparent_24%),linear-gradient(180deg,#0f172a_0%,#08233d_45%,#06172a_100%)]" />
          <div className="absolute -right-28 top-[-130px] size-[390px] rounded-full border-[70px] border-white/7" />
          <div className="absolute -bottom-28 -left-24 size-[290px] rounded-full border-[54px] border-white/7" />
          <div className="absolute inset-0 bg-[linear-gradient(transparent_96%,rgba(255,255,255,0.035)_96%),linear-gradient(90deg,transparent_96%,rgba(255,255,255,0.025)_96%)] bg-[size:26px_26px] opacity-50" />

          <div className="relative flex h-full w-full max-w-xl flex-col items-center px-10 py-14">
            <div className="flex flex-1 flex-col items-center justify-center text-center">
              <div className="mb-10 flex items-center justify-center gap-5">
                <img
                  src="/verbo-brand/verbo-mark-transparent.png"
                  alt=""
                  className="h-24 w-32 object-contain drop-shadow-2xl"
                  aria-hidden="true"
                />
                <div className="text-left">
                  <p className="text-6xl font-bold leading-none tracking-tight">Verbo+</p>
                  <p className="mt-3 bg-[linear-gradient(90deg,#8b5cf6,#0ea5e9,#06b6d4)] bg-clip-text text-lg font-semibold text-transparent">
                    {t('login.heroDescription')}
                  </p>
                </div>
              </div>

              <h1 className="text-5xl font-bold leading-none tracking-tight">{t('login.brandHeadline')}</h1>
              <p className="mt-6 max-w-sm text-lg leading-8 text-slate-300">{t('login.brandDescription')}</p>

              <div className="mt-14 flex items-center justify-center gap-3" aria-hidden="true">
                <span className="h-2 w-8 rounded-full bg-white" />
                <span className="size-2 rounded-full bg-white/35" />
                <span className="size-2 rounded-full bg-white/35" />
              </div>
            </div>

            <div className="relative w-full border-t border-white/18 pt-9">
              <div className="grid grid-cols-4 gap-6 text-center text-xs font-medium uppercase tracking-[0.14em] text-slate-300">
                <span>{t('login.featureManagement')}</span>
                <span>{t('login.featureCommunication')}</span>
                <span>{t('login.featureDecisions')}</span>
                <span>{t('login.featureCare')}</span>
              </div>
            </div>
          </div>
        </aside>

        <main className="flex min-h-screen flex-col bg-white">
          <div className="flex flex-1 items-center justify-center px-6 py-10 sm:px-10">
            <section className="w-full max-w-[430px]">
              <div className="mb-10 flex justify-center lg:hidden">
                <div className="flex items-center gap-3">
                  <img src="/verbo-brand/verbo-mark-light-transparent.png" alt="" className="h-14 w-16 object-contain" aria-hidden="true" />
                  <span className="text-3xl font-bold tracking-tight text-slate-950">Verbo+</span>
                </div>
              </div>

              <div className="mb-9">
                <h2 className="text-3xl font-bold tracking-tight text-[#1e4f82]">{t('login.welcomeTitle')}</h2>
                <p className="mt-4 text-base leading-7 text-slate-500">{t('login.welcomeSubtitle')}</p>
              </div>

              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="space-y-2">
                  <Label htmlFor="email" className="text-sm font-bold uppercase tracking-wide text-slate-500">
                    {t('login.email')}
                  </Label>
                  <div className="relative">
                    <Mail className="absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-400" />
                    <Input
                      id="email"
                      name="email"
                      type="email"
                      value={formData.email}
                      onChange={handleChange}
                      placeholder={t('login.emailPlaceholder')}
                      className="h-14 rounded-lg border-slate-200 bg-white pl-12 pr-4 text-base shadow-none transition placeholder:text-slate-500 focus-visible:border-[#2563eb] focus-visible:ring-[#2563eb]/20"
                      required
                      autoComplete="email"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="senha" className="text-sm font-bold uppercase tracking-wide text-slate-500">
                    {t('login.password')}
                  </Label>
                  <div className="relative">
                    <Lock className="absolute left-4 top-1/2 size-5 -translate-y-1/2 text-slate-400" />
                    <Input
                      id="senha"
                      name="senha"
                      type={showPassword ? 'text' : 'password'}
                      value={formData.senha}
                      onChange={handleChange}
                      placeholder={t('login.passwordPlaceholder')}
                      className="h-14 rounded-lg border-slate-200 bg-white pl-12 pr-12 text-base tracking-[0.12em] shadow-none transition placeholder:text-slate-500 focus-visible:border-[#2563eb] focus-visible:ring-[#2563eb]/20"
                      required
                      autoComplete="current-password"
                    />
                    <button
                      type="button"
                      className="absolute right-3 top-1/2 inline-flex size-8 -translate-y-1/2 items-center justify-center rounded-md text-slate-500 transition hover:bg-slate-100 hover:text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2563eb]/25"
                      onClick={() => setShowPassword((current) => !current)}
                      aria-label={showPassword ? t('login.hidePassword') : t('login.showPassword')}
                    >
                      {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                    </button>
                  </div>
                </div>

                <Button
                  type="submit"
                  className="mt-1 h-14 w-full rounded-lg bg-[#2563eb] text-base font-bold text-white shadow-none hover:bg-[#1d4ed8]"
                  disabled={loading}
                >
                  {loading ? t('login.submitting') : t('login.submit')}
                </Button>
              </form>
            </section>
          </div>

          <footer className="pb-8 text-center text-sm text-slate-300">
            {t('login.footer')}
          </footer>
        </main>
      </div>
      <Toaster />
    </>
  );
}
