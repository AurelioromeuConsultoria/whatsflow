import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Users, User, CalendarDays, ClipboardList, Handshake, Cake, ChevronRight,
  Plus, MessageSquarePlus, CalendarPlus, ArrowRight, Circle, CheckCircle2, TrendingUp,
} from 'lucide-react';
import {
  PieChart, Pie, Cell, AreaChart, Area, BarChart, Bar, XAxis, YAxis, CartesianGrid, Legend,
  ResponsiveContainer, Tooltip as RechartsTooltip,
} from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { dashboardApi, eventosApi, normalizeEvento, comunicacaoCampanhasApi } from '@/lib/api';
import { useAuth } from '@/context/AuthContext';
import { ErrorPage } from '@/components/ui/error-message';
import { Skeleton } from '@/components/ui/skeleton';
import { useTranslation } from 'react-i18next';
import { formatDate } from '@/lib/formatters';

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

// Tile de KPI clicável (drill-down) com tendência e delta do mês.
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
          <span className="text-2xl font-bold leading-tight">{value}</span>
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
  const [estatisticas, setEstatisticas] = useState(null);
  const [serie, setSerie] = useState(null);
  const [eventos, setEventos] = useState(null);
  const [comunicacao, setComunicacao] = useState(null);
  const [meses, setMeses] = useState(6);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadDashboard();
  }, []);

  // Série recarrega quando o período muda.
  useEffect(() => {
    dashboardApi.getSerie(meses).then((r) => setSerie(r.data)).catch(() => setSerie(null));
  }, [meses]);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      setError(null);
      const statsResp = await dashboardApi.getEstatisticas(); // essencial (bloqueia)
      setEstatisticas(statsResp.data);

      // Enriquecimentos (best-effort; falham sem quebrar a página).
      eventosApi.getAll().then((r) => setEventos((r.data || []).map(normalizeEvento))).catch(() => setEventos([]));
      comunicacaoCampanhasApi.getStats().then((r) => setComunicacao(r.data)).catch(() => setComunicacao(null));
    } catch (err) {
      console.error('Error loading dashboard:', err);
      setError(t('dashboard.errorLoad'));
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

  if (error) {
    return <ErrorPage message={error} onRetry={loadDashboard} />;
  }

  const stats = estatisticas || {
    totalVisitantes: 0, mensagensAgendadas: 0, mensagensEnviadas: 0, configuracoesAtivas: 0,
    totalPessoas: 0, totalEventos: 0, totalInscricoes: 0, totalVoluntarios: 0,
    totalAniversariantesProximos: 0, proximosAniversariantes: [],
  };

  const serieKey = (key) => (serie?.length ? serie.map((p) => p[key]) : null);
  const delta = (key) => {
    if (!serie || serie.length < 2) return null;
    return serie[serie.length - 1][key] - serie[serie.length - 2][key];
  };

  const tiles = [
    { to: '/pessoas', icon: User, value: stats.totalPessoas, label: t('dashboard.cards.totalPeople.title'), series: serieKey('pessoas'), delta: delta('pessoas') },
    { to: '/visitantes', icon: Users, value: stats.totalVisitantes, label: t('dashboard.cards.totalVisitors.title'), series: serieKey('visitantes'), delta: delta('visitantes') },
    { to: '/voluntarios', icon: Handshake, value: stats.totalVoluntarios, label: t('dashboard.cards.totalVolunteers.title'), series: serieKey('voluntarios'), delta: delta('voluntarios') },
    { to: '/eventos', icon: CalendarDays, value: stats.totalEventos, label: t('dashboard.cards.totalEvents.title'), series: serieKey('eventos'), delta: delta('eventos') },
    { to: '/eventos', icon: ClipboardList, value: stats.totalInscricoes, label: t('dashboard.cards.totalRegistrations.title'), series: serieKey('inscricoes'), delta: delta('inscricoes') },
    { to: '/pessoas/aniversariantes', icon: Cake, value: stats.totalAniversariantesProximos, label: t('dashboard.cards.upcomingBirthdays.title'), series: null, delta: null },
  ];

  // Saúde de entrega das campanhas (WhatsApp).
  const entregues = comunicacao?.entregasEnviadas || 0;
  const pendentes = comunicacao?.entregasPendentes || 0;
  const falhas = comunicacao?.entregasComFalha || 0;
  const totalEntregas = entregues + pendentes + falhas;
  const comunicacaoData = [
    { name: t('dashboard.communication.delivered', { defaultValue: 'Entregues' }), value: entregues, color: '#7c3aed' },
    { name: t('dashboard.communication.pending', { defaultValue: 'Pendentes' }), value: pendentes, color: '#06b6d4' },
    { name: t('dashboard.communication.failed', { defaultValue: 'Falhas' }), value: falhas, color: '#ef4444' },
  ];

  const aniversariantes = (stats.proximosAniversariantes || []).slice(0, 5);

  const hoje = new Date();
  hoje.setHours(0, 0, 0, 0);
  const proximosEventos = (eventos || [])
    .filter((e) => e.dataInicio && new Date(e.dataInicio) >= hoje)
    .sort((a, b) => new Date(a.dataInicio) - new Date(b.dataInicio))
    .slice(0, 5);

  // Checklist de ativação — derivado dos dados; some quando concluído.
  const setupSteps = [
    { done: stats.totalPessoas > 0, label: t('dashboard.setup.people', { defaultValue: 'Cadastre suas pessoas' }), to: '/pessoas/novo' },
    { done: stats.configuracoesAtivas > 0, label: t('dashboard.setup.messages', { defaultValue: 'Configure mensagens de boas-vindas' }), to: '/configuracoes-mensagens' },
    { done: stats.totalEventos > 0, label: t('dashboard.setup.event', { defaultValue: 'Crie seu primeiro evento' }), to: '/eventos/novo' },
    { done: stats.mensagensEnviadas > 0 || entregues > 0, label: t('dashboard.setup.firstMessage', { defaultValue: 'Envie sua primeira campanha' }), to: '/comunicacao/campanhas/nova' },
  ];
  const setupConcluido = setupSteps.filter((s) => s.done).length;
  const mostrarSetup = setupConcluido < setupSteps.length;

  // Saudação personalizada.
  const horaAtual = new Date().getHours();
  const saudacao = horaAtual < 12
    ? t('dashboard.greeting.morning', { defaultValue: 'Bom dia' })
    : horaAtual < 18
      ? t('dashboard.greeting.afternoon', { defaultValue: 'Boa tarde' })
      : t('dashboard.greeting.evening', { defaultValue: 'Boa noite' });
  const primeiroNome = (usuario?.nome || '').trim().split(' ')[0];

  return (
    <div className="space-y-6">
      {/* Cabeçalho + ações rápidas */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold">
            {saudacao}{primeiroNome ? `, ${primeiroNome}` : ''} 👋
          </h1>
          <p className="text-sm text-muted-foreground">{t('dashboard.subtitle')}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button asChild size="sm" variant="outline">
            <Link to="/pessoas/novo"><Plus className="h-4 w-4" />{t('dashboard.actions.newPerson', { defaultValue: 'Nova pessoa' })}</Link>
          </Button>
          <Button asChild size="sm" variant="outline">
            <Link to="/eventos/novo"><CalendarPlus className="h-4 w-4" />{t('dashboard.actions.newEvent', { defaultValue: 'Novo evento' })}</Link>
          </Button>
          <Button asChild size="sm">
            <Link to="/comunicacao/campanhas/nova"><MessageSquarePlus className="h-4 w-4" />{t('dashboard.actions.newCampaign', { defaultValue: 'Nova campanha' })}</Link>
          </Button>
        </div>
      </div>

      {/* Checklist de ativação (só enquanto não concluído) */}
      {mostrarSetup && (
        <Card className="border-primary/30 bg-primary/5">
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">{t('dashboard.setup.title', { defaultValue: 'Primeiros passos no Verbo+' })}</CardTitle>
            <span className="text-sm font-medium text-muted-foreground">{setupConcluido}/{setupSteps.length}</span>
          </CardHeader>
          <CardContent className="grid gap-2 sm:grid-cols-2">
            {setupSteps.map((s) => (
              <Link
                key={s.label}
                to={s.to}
                className={`flex items-center gap-2 rounded-md px-2 py-1.5 text-sm transition hover:bg-accent ${s.done ? 'text-muted-foreground' : 'font-medium'}`}
              >
                {s.done
                  ? <CheckCircle2 className="h-4 w-4 shrink-0 text-primary" />
                  : <Circle className="h-4 w-4 shrink-0 text-muted-foreground/50" />}
                <span className={s.done ? 'line-through' : ''}>{s.label}</span>
              </Link>
            ))}
          </CardContent>
        </Card>
      )}

      {/* KPIs clicáveis com tendência + delta */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6">
        {tiles.map((tile, i) => <StatTile key={i} {...tile} />)}
      </div>

      {/* Análise: período + dois gráficos */}
      {serie?.length > 1 && (
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="flex items-center gap-2 text-sm font-semibold text-muted-foreground">
              <TrendingUp className="h-4 w-4 text-primary" />
              {t('dashboard.analysis.title', { defaultValue: 'Análise' })}
            </h2>
            <div className="inline-flex rounded-lg border bg-card p-0.5 text-sm">
              {[3, 6, 12].map((m) => (
                <button
                  key={m}
                  type="button"
                  onClick={() => setMeses(m)}
                  className={`rounded-md px-3 py-1 font-medium transition ${meses === m ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:text-foreground'}`}
                >
                  {m}m
                </button>
              ))}
            </div>
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            {/* Crescimento */}
            <Card>
              <CardHeader><CardTitle className="text-base">{t('dashboard.growth.title', { defaultValue: 'Crescimento' })}</CardTitle></CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={240}>
                  <AreaChart data={serie} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                    <defs>
                      <linearGradient id="gPessoas" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#7c3aed" stopOpacity={0.35} />
                        <stop offset="100%" stopColor="#7c3aed" stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="gVisitantes" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#06b6d4" stopOpacity={0.35} />
                        <stop offset="100%" stopColor="#06b6d4" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="currentColor" className="text-border" />
                    <XAxis dataKey="mes" tickLine={false} axisLine={false} fontSize={12} />
                    <YAxis tickLine={false} axisLine={false} fontSize={12} width={34} allowDecimals={false} />
                    <RechartsTooltip />
                    <Legend />
                    <Area type="monotone" dataKey="pessoas" name={t('dashboard.cards.totalPeople.title')} stroke="#7c3aed" strokeWidth={2} fill="url(#gPessoas)" />
                    <Area type="monotone" dataKey="visitantes" name={t('dashboard.cards.totalVisitors.title')} stroke="#06b6d4" strokeWidth={2} fill="url(#gVisitantes)" />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>

            {/* Mensagens enviadas por mês */}
            <Card>
              <CardHeader><CardTitle className="text-base">{t('dashboard.messagesChart.title', { defaultValue: 'Mensagens enviadas por mês' })}</CardTitle></CardHeader>
              <CardContent>
                <ResponsiveContainer width="100%" height={240}>
                  <BarChart data={serie} margin={{ top: 8, right: 8, left: -18, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="currentColor" className="text-border" />
                    <XAxis dataKey="mes" tickLine={false} axisLine={false} fontSize={12} />
                    <YAxis tickLine={false} axisLine={false} fontSize={12} width={34} allowDecimals={false} />
                    <RechartsTooltip />
                    <Bar dataKey="mensagensEnviadas" name={t('dashboard.cards.sentMessages.title')} fill="#8b5cf6" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {/* Widgets */}
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {/* Comunicação — saúde de entrega */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">{t('dashboard.communication.title', { defaultValue: 'Comunicação (WhatsApp)' })}</CardTitle>
            <Link to="/comunicacao/campanhas" className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline">
              {t('dashboard.communication.viewCampaigns', { defaultValue: 'Ver campanhas' })}
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent>
            {totalEntregas === 0 ? (
              <p className="py-10 text-center text-sm text-muted-foreground">
                {t('dashboard.communication.empty', { defaultValue: 'Nenhuma entrega registrada ainda.' })}
              </p>
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

        {/* Próximos aniversários */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">{t('dashboard.birthdays.title')}</CardTitle>
            <Link to="/pessoas/aniversariantes/campanha" className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline">
              {t('dashboard.birthdays.campaign', { defaultValue: 'Campanha' })}
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent>
            {aniversariantes.length ? (
              <div className="divide-y">
                {aniversariantes.map((p) => (
                  <Link
                    key={p.id}
                    to={`/pessoas/${p.id}`}
                    className="-mx-2 flex items-center justify-between gap-3 rounded-md px-2 py-2.5 transition first:pt-0 hover:bg-accent/50"
                  >
                    <div className="flex min-w-0 items-center gap-3">
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-xs font-semibold text-primary">
                        {(p.nome || '?').charAt(0).toUpperCase()}
                      </span>
                      <div className="min-w-0">
                        <div className="truncate text-sm font-medium">{p.nome}</div>
                        <div className="text-xs text-muted-foreground">{formatDate(p.proximoAniversario)}</div>
                      </div>
                    </div>
                    <span className="shrink-0 rounded-full bg-secondary px-2 py-0.5 text-xs font-semibold text-secondary-foreground">
                      {t('dashboard.birthdays.days', { count: p.diasParaAniversario })}
                    </span>
                  </Link>
                ))}
              </div>
            ) : (
              <p className="py-10 text-center text-sm text-muted-foreground">{t('dashboard.birthdays.empty')}</p>
            )}
          </CardContent>
        </Card>

        {/* Próximos eventos */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <CardTitle className="text-base">{t('dashboard.events.title', { defaultValue: 'Próximos eventos' })}</CardTitle>
            <Link to="/eventos" className="inline-flex items-center gap-1 text-sm font-medium text-primary hover:underline">
              {t('dashboard.events.agenda', { defaultValue: 'Agenda' })}
              <ArrowRight className="h-3.5 w-3.5" />
            </Link>
          </CardHeader>
          <CardContent>
            {proximosEventos.length ? (
              <div className="divide-y">
                {proximosEventos.map((e) => (
                  <Link
                    key={e.id}
                    to={`/eventos/${e.id}/editar`}
                    className="-mx-2 flex items-center justify-between gap-3 rounded-md px-2 py-2.5 transition first:pt-0 hover:bg-accent/50"
                  >
                    <div className="flex min-w-0 items-center gap-3">
                      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
                        <CalendarDays className="h-4 w-4" />
                      </span>
                      <div className="truncate text-sm font-medium">{e.titulo || t('dashboard.events.untitled', { defaultValue: 'Evento' })}</div>
                    </div>
                    <span className="shrink-0 text-xs text-muted-foreground">{formatDate(e.dataInicio)}</span>
                  </Link>
                ))}
              </div>
            ) : (
              <p className="py-10 text-center text-sm text-muted-foreground">{t('dashboard.events.empty', { defaultValue: 'Nenhum evento futuro.' })}</p>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
