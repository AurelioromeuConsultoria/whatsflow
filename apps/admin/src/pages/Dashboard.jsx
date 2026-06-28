import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Users, UserCheck, CheckCircle2, CalendarClock, Send, Settings,
  ChevronRight, Plus, MessageSquarePlus, ArrowRight, TrendingUp, Circle,
} from 'lucide-react';
import {
  PieChart, Pie, Cell, AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Legend,
  ResponsiveContainer, Tooltip as RechartsTooltip,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { dashboardApi, comunicacaoCampanhasApi } from '@/lib/api';
import { useAuth } from '@/context/AuthContext';
import { ErrorPage } from '@/components/ui/error-message';
import { Skeleton } from '@/components/ui/skeleton';
import { useTranslation } from 'react-i18next';

// Mini-gráfico de tendência (SVG leve, sem peso de lib por tile).
function Sparkline({ data }) {
  if (!data || data.length < 2) return <div className="h-6" />;
  const w = 88, h = 24;
  const max = Math.max(...data), min = Math.min(...data);
  const range = max - min || 1;
  const pts = data
    .map((v, i) => `${((i / (data.length - 1)) * w).toFixed(1)},${(h - ((v - min) / range) * h).toFixed(1)}`)
    .join(' ');
  return (
    <svg width="100%" height={h} viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none" className="text-primary/40">
      <polyline points={pts} fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" strokeLinecap="round" />
    </svg>
  );
}

function StatTile({ to, icon, label, value, series, delta }) {
  const Icon = icon;
  return (
    <Link
      to={to}
      className="group flex flex-col gap-2 rounded-xl border bg-card p-4 transition hover:border-primary/40 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
    >
      <div className="flex items-center justify-between">
        <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary">
          <Icon className="h-5 w-5" />
        </span>
        <ChevronRight className="h-4 w-4 text-muted-foreground/40 transition group-hover:translate-x-0.5 group-hover:text-primary" />
      </div>
      <div>
        <div className="flex items-baseline gap-2">
          <span className="text-2xl font-bold leading-tight">{value ?? 0}</span>
          {delta > 0 && <span className="text-xs font-semibold text-emerald-600">+{delta}</span>}
        </div>
        <div className="text-sm font-medium">{label}</div>
      </div>
      <Sparkline data={series} />
    </Link>
  );
}

export default function Dashboard() {
  const { t } = useTranslation();
  const { usuario } = useAuth();
  const [stats, setStats] = useState(null);
  const [serie, setSerie] = useState(null);
  const [comunicacao, setComunicacao] = useState(null);
  const [meses, setMeses] = useState(6);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => { loadDashboard(); }, []);

  useEffect(() => {
    dashboardApi.getSerie(meses).then((r) => setSerie(r.data)).catch(() => setSerie(null));
  }, [meses]);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      setError(null);
      const statsResp = await dashboardApi.getEstatisticas();
      setStats(statsResp.data);
      comunicacaoCampanhasApi.getStats().then((r) => setComunicacao(r.data)).catch(() => setComunicacao(null));
    } catch (err) {
      console.error('Error loading dashboard:', err);
      setError(t('dashboard.errorLoad', { defaultValue: 'Erro ao carregar o painel.' }));
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-9 w-64" />
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
          {[...Array(6)].map((_, i) => <Skeleton key={i} className="h-32 rounded-xl" />)}
        </div>
        <Skeleton className="h-64 rounded-xl" />
      </div>
    );
  }

  if (error) return <ErrorPage message={error} onRetry={loadDashboard} />;

  const s = stats || {
    totalContatos: 0, contatosAtivos: 0, contatosOptIn: 0,
    mensagensAgendadas: 0, mensagensEnviadas: 0, configuracoesAtivas: 0,
  };

  const serieKey = (key) => (serie?.length ? serie.map((p) => p[key]) : null);
  const delta = (key) => {
    if (!serie || serie.length < 2) return null;
    return serie[serie.length - 1][key] - serie[serie.length - 2][key];
  };

  const tiles = [
    { to: '/contatos', icon: Users, value: s.totalContatos, label: 'Contatos', series: serieKey('contatos'), delta: delta('contatos') },
    { to: '/contatos', icon: UserCheck, value: s.contatosAtivos, label: 'Contatos ativos', series: null, delta: null },
    { to: '/contatos', icon: CheckCircle2, value: s.contatosOptIn, label: 'Com opt-in', series: null, delta: null },
    { to: '/mensagens-agendadas', icon: CalendarClock, value: s.mensagensAgendadas, label: 'Mensagens agendadas', series: null, delta: null },
    { to: '/comunicacao/logs', icon: Send, value: s.mensagensEnviadas, label: 'Mensagens enviadas', series: serieKey('mensagensEnviadas'), delta: delta('mensagensEnviadas') },
    { to: '/configuracoes-mensagens', icon: Settings, value: s.configuracoesAtivas, label: 'Configurações ativas', series: null, delta: null },
  ];

  const entregues = comunicacao?.entregasEnviadas || 0;
  const pendentes = comunicacao?.entregasPendentes || 0;
  const falhas = comunicacao?.entregasComFalha || 0;
  const totalEntregas = entregues + pendentes + falhas;
  const comunicacaoData = [
    { name: 'Enviadas', value: entregues, color: '#25d366' },
    { name: 'Pendentes', value: pendentes, color: '#128c7e' },
    { name: 'Falhas', value: falhas, color: '#ef4444' },
  ];

  const setupSteps = [
    { done: s.totalContatos > 0, label: 'Cadastre seus contatos', to: '/contatos/novo' },
    { done: s.configuracoesAtivas > 0, label: 'Conecte uma conta WhatsApp', to: '/whatsapp/contas/novo' },
    { done: s.mensagensEnviadas > 0 || entregues > 0, label: 'Envie sua primeira campanha', to: '/comunicacao/campanhas/nova' },
  ];
  const setupConcluido = setupSteps.filter((step) => step.done).length;
  const mostrarSetup = setupConcluido < setupSteps.length;

  const horaAtual = new Date().getHours();
  const saudacao = horaAtual < 12 ? 'Bom dia' : horaAtual < 18 ? 'Boa tarde' : 'Boa noite';
  const primeiroNome = (usuario?.nome || '').trim().split(' ')[0];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">{saudacao}{primeiroNome ? `, ${primeiroNome}` : ''} 👋</h1>
          <p className="text-sm text-muted-foreground">Visão geral do seu workspace WhatsApp.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button asChild size="sm" variant="outline">
            <Link to="/contatos/novo"><Plus className="h-4 w-4" /> Novo contato</Link>
          </Button>
          <Button asChild size="sm">
            <Link to="/comunicacao/campanhas/nova"><MessageSquarePlus className="h-4 w-4" /> Nova campanha</Link>
          </Button>
        </div>
      </div>

      {mostrarSetup && (
        <Card className="border-primary/30 bg-primary/5">
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">Primeiros passos no WhatsFlow</CardTitle>
            <span className="text-sm font-medium text-muted-foreground">{setupConcluido}/{setupSteps.length}</span>
          </CardHeader>
          <CardContent className="grid gap-2 sm:grid-cols-3">
            {setupSteps.map((step) => (
              <Link key={step.label} to={step.to} className={`flex items-center gap-2 rounded-md px-2 py-1.5 text-sm transition hover:bg-accent ${step.done ? 'text-muted-foreground' : 'font-medium'}`}>
                {step.done ? <CheckCircle2 className="h-4 w-4 shrink-0 text-primary" /> : <Circle className="h-4 w-4 shrink-0 text-muted-foreground/50" />}
                <span className={step.done ? 'line-through' : ''}>{step.label}</span>
              </Link>
            ))}
          </CardContent>
        </Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        {tiles.map((tile, i) => <StatTile key={i} {...tile} />)}
      </div>

      {serie?.length > 1 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-sm font-semibold text-muted-foreground">
              <TrendingUp className="h-4 w-4 text-primary" /> Análise
            </h2>
            <div className="inline-flex rounded-lg border bg-card p-0.5 text-sm">
              {[3, 6, 12].map((m) => (
                <button key={m} type="button" onClick={() => setMeses(m)}
                  className={`rounded-md px-3 py-1 font-medium transition ${meses === m ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground'}`}>
                  {m}m
                </button>
              ))}
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader><CardTitle className="text-base">Crescimento de contatos</CardTitle></CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={240}>
                  <AreaChart data={serie} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                    <defs>
                      <linearGradient id="gContatos" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#25d366" stopOpacity={0.35} />
                        <stop offset="100%" stopColor="#25d366" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="currentColor" className="text-border" />
                    <XAxis dataKey="mes" tickLine={false} axisLine={false} fontSize={12} />
                    <YAxis tickLine={false} axisLine={false} fontSize={12} width={34} allowDecimals={false} />
                    <RechartsTooltip />
                    <Legend />
                    <Area type="monotone" dataKey="contatos" name="Contatos" stroke="#25d366" strokeWidth={2} fill="url(#gContatos)" />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            <Card>
              <CardHeader><CardTitle className="text-base">Mensagens enviadas por mês</CardTitle></CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={240}>
                  <BarChart data={serie} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="currentColor" className="text-border" />
                    <XAxis dataKey="mes" tickLine={false} axisLine={false} fontSize={12} />
                    <YAxis tickLine={false} axisLine={false} fontSize={12} width={34} allowDecimals={false} />
                    <RechartsTooltip />
                    <Bar dataKey="mensagensEnviadas" name="Mensagens enviadas" fill="#128c7e" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">Comunicação (WhatsApp)</CardTitle>
            <Link to="/comunicacao/campanhas" className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline">
              Ver campanhas <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent>
            {totalEntregas === 0 ? (
              <p className="py-10 text-center text-sm text-muted-foreground">Nenhuma entrega registrada ainda.</p>
            ) : (
              <div className="flex items-center gap-4">
                <ResponsiveContainer width={120} height={120}>
                  <PieChart>
                    <Pie data={comunicacaoData} dataKey="value" nameKey="name" innerRadius={38} outerRadius={58} paddingAngle={2} strokeWidth={0}>
                      {comunicacaoData.map((d) => <Cell key={d.name} fill={d.color} />)}
                    </Pie>
                    <RechartsTooltip />
                  </PieChart>
                </ResponsiveContainer>
                <div className="space-y-2">
                  {comunicacaoData.map((d) => (
                    <div key={d.name} className="flex items-center gap-2">
                      <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: d.color }} />
                      <span className="text-xl font-bold">{d.value}</span>
                      <span className="text-xs text-muted-foreground">{d.name}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">Campanhas</CardTitle>
            <Link to="/comunicacao/campanhas" className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline">
              Gerenciar <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent className="grid grid-cols-2 gap-4">
            <div className="rounded-lg border p-4">
              <div className="text-sm text-muted-foreground">Total de campanhas</div>
              <div className="text-2xl font-bold">{comunicacao?.totalCampanhas ?? 0}</div>
            </div>
            <div className="rounded-lg border p-4">
              <div className="text-sm text-muted-foreground">Rascunhos</div>
              <div className="text-2xl font-bold">{comunicacao?.campanhasRascunho ?? 0}</div>
            </div>
            <div className="rounded-lg border p-4">
              <div className="text-sm text-muted-foreground">Agendadas</div>
              <div className="text-2xl font-bold">{comunicacao?.campanhasAgendadas ?? 0}</div>
            </div>
            <div className="rounded-lg border p-4">
              <div className="text-sm text-muted-foreground">Entregas com falha</div>
              <div className="text-2xl font-bold">{comunicacao?.entregasComFalha ?? 0}</div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
