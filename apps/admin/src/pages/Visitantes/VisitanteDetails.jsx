import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Edit, Phone, Mail, Calendar, MessageSquare, RefreshCcw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { visitantesApi, mensagensAgendadasApi } from '@/lib/api';
import { formatDate } from '@/lib/formatters';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function VisitanteDetails() {
  const { id } = useParams();
  const { t } = useTranslation();
  const [visitante, setVisitante] = useState(null);
  const [mensagens, setMensagens] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [regenerando, setRegenerando] = useState(false);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const visitanteResponse = await visitantesApi.getById(id);
      const visitante = visitanteResponse.data;
      
      setVisitante(visitante);
      
      // Tentar carregar mensagens se o endpoint existir
      try {
        const mensagensResponse = await mensagensAgendadasApi.getAll();
        const mensagensDoVisitante = mensagensResponse.data.filter(
          msg => msg.visitanteId === parseInt(id)
        );
        setMensagens(mensagensDoVisitante);
      } catch (err) {
        // Ignorar erro se o endpoint não existir
        console.log(t('visitors.details.messagesUnavailableLog'));
      }
    } catch (err) {
      setError(t('visitors.details.errorLoad'));
      console.error('Erro ao carregar dados:', err);
    } finally {
      setLoading(false);
    }
  };

  const getStatusBadge = (status) => {
    const value = Number(status);
    if (value === 1) return <Badge variant="secondary">{t('visitors.details.messageStatus.scheduled')}</Badge>;
    if (value === 2) return <Badge variant="secondary">{t('visitors.details.messageStatus.ready')}</Badge>;
    if (value === 3) return <Badge variant="default">{t('visitors.details.messageStatus.sent')}</Badge>;
    if (value === 4) return <Badge variant="destructive">{t('visitors.details.messageStatus.error')}</Badge>;
    if (value === 5) return <Badge variant="outline">{t('visitors.details.messageStatus.canceled')}</Badge>;
    if (value === 6) return <Badge variant="secondary">{t('visitors.details.messageStatus.processing')}</Badge>;
    return <Badge variant="secondary">{String(status)}</Badge>;
  };

  const handleRegerarMensagens = async () => {
    try {
      setRegenerando(true);
      const res = await visitantesApi.regerarMensagens(id);
      toast.success(
        t('visitors.form.regenerateSuccess', {
          created: res.data?.mensagensCriadas ?? 0,
          canceled: res.data?.mensagensCanceladas ?? 0,
        })
      );
      await loadData();
    } catch (err) {
      const msg = typeof err.response?.data === 'string'
        ? err.response.data
        : (err.response?.data?.message || err.response?.data?.error || t('visitors.form.regenerateError'));
      toast.error(msg);
      console.error(err);
    } finally {
      setRegenerando(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [id]);

  if (loading) {
    return <LoadingPage text={t('visitors.form.loading')} />;
  }

  if (error) {
    return <ErrorPage message={error} onRetry={loadData} />;
  }

  if (!visitante) {
    return <ErrorPage message={t('visitors.details.notFound')} />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Button variant="ghost" asChild>
            <Link to="/visitantes">
              <ArrowLeft className="h-4 w-4 mr-2" />
              {t('actions.back')}
            </Link>
          </Button>
          <div>
            <h1 className="text-3xl font-bold">
              {visitante.nome || visitante.pessoa?.nome || t('visitors.title')}
            </h1>
            <p className="text-muted-foreground">
              {t('visitors.details.subtitle')}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={handleRegerarMensagens} disabled={regenerando}>
            <RefreshCcw className="h-4 w-4 mr-2" />
            {regenerando ? t('visitors.form.regenerating') : t('visitors.form.regenerateMessages')}
          </Button>
          <Button asChild>
            <Link to={`/visitantes/${id}/editar`}>
              <Edit className="h-4 w-4 mr-2" />
              {t('visitors.edit')}
            </Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>{t('visitors.details.visitData')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center space-x-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                <Calendar className="h-5 w-5 text-primary" />
              </div>
              <div>
                <p className="text-sm font-medium">{t('visitors.form.fields.visitDate')}</p>
                <p className="text-sm text-muted-foreground">
                  {formatDate(visitante.dataVisita)}
                </p>
              </div>
            </div>

            {visitante.observacoes && (
              <div>
                <p className="text-sm font-medium mb-2">{t('visitors.form.fields.notes')}</p>
                <p className="text-sm text-muted-foreground bg-muted p-3 rounded-lg">
                  {visitante.observacoes}
                </p>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t('visitors.details.personData')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm font-medium">{t('visitors.form.fields.name')}</p>
              <p className="text-sm text-muted-foreground">
                {visitante.nome || visitante.pessoa?.nome || '-'}
              </p>
            </div>

            {visitante.email && (
              <div className="flex items-center space-x-2">
                <div>
                  <p className="text-sm font-medium">{t('visitors.form.fields.email')}</p>
                  <p className="text-sm text-muted-foreground">{visitante.email}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => window.open(`mailto:${visitante.email}`)}
                >
                  <Mail className="h-4 w-4" />
                </Button>
              </div>
            )}

            {visitante.telefone && (
              <div>
                <p className="text-sm font-medium">{t('visitors.form.fields.phone')}</p>
                <p className="text-sm text-muted-foreground">{visitante.telefone}</p>
              </div>
            )}

            {visitante.whatsApp && (
              <div className="flex items-center space-x-2">
                <div>
                  <p className="text-sm font-medium">{t('visitors.form.fields.whatsapp')}</p>
                  <p className="text-sm text-muted-foreground">{visitante.whatsApp}</p>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => window.open(`https://wa.me/55${visitante.whatsApp.replace(/\D/g, '')}`)}
                >
                  <Phone className="h-4 w-4" />
                </Button>
              </div>
            )}

            {visitante.perfis && visitante.perfis.length > 0 && (
              <div>
                <p className="text-sm font-medium mb-2">{t('visitors.details.profiles')}</p>
                <div className="flex flex-wrap gap-1">
                  {visitante.perfis.map((perfil, idx) => (
                      <Badge key={idx} variant="secondary">
                        {perfil}
                      </Badge>
                  ))}
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <MessageSquare className="h-5 w-5" />
              <span>{t('visitors.details.scheduledMessages')}</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            {mensagens.length === 0 ? (
              <p className="text-sm text-muted-foreground text-center py-4">
                {t('visitors.details.noScheduledMessages')}
              </p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>{t('visitors.details.table.type')}</TableHead>
                    <TableHead>{t('visitors.details.table.sendDate')}</TableHead>
                    <TableHead>{t('visitors.details.table.status')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mensagens.map((mensagem) => (
                    <TableRow key={mensagem.id}>
                      <TableCell className="font-medium">
                        {mensagem.configuracaoMensagem?.titulo || t('visitors.details.messageFallback')}
                      </TableCell>
                      <TableCell>
                        {formatDate(mensagem.dataEnvio)}
                      </TableCell>
                      <TableCell>
                        {getStatusBadge(mensagem.status)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
