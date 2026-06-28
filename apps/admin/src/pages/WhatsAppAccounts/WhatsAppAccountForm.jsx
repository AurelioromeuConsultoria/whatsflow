import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { StatusBadge } from '@/components/ui/status-badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { whatsappAccountsApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';

const PROVIDERS = [
  { value: 0, label: 'Fake (testes)' },
  { value: 1, label: 'Cloud API (oficial)' },
  { value: 2, label: 'Evolution API' },
  { value: 3, label: 'Twilio' },
  { value: 4, label: 'Zenvia' },
  { value: 99, label: 'Outro' },
];

const STATUSES = [
  { value: 1, label: 'Ativa' },
  { value: 2, label: 'Inativa' },
  { value: 3, label: 'Erro de configuração' },
];

const EMPTY = {
  nome: '',
  provider: 0,
  phoneNumberId: '',
  businessAccountId: '',
  status: 1,
  configuracoesJson: '',
};

export default function WhatsAppAccountForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState(EMPTY);
  // Tokens/secrets são write-only: null = mantém atual, string vazia = remove, valor = define.
  const [accessToken, setAccessToken] = useState('');
  const [webhookSecret, setWebhookSecret] = useState('');
  const [possuiAccessToken, setPossuiAccessToken] = useState(false);
  const [possuiWebhookSecret, setPossuiWebhookSecret] = useState(false);
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(isEditing);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!isEditing) return;
    const load = async () => {
      try {
        setPageLoading(true);
        setError(null);
        const res = await whatsappAccountsApi.getById(id);
        const a = res.data;
        setFormData({
          nome: a.nome || '',
          provider: Number(a.provider) || 0,
          phoneNumberId: a.phoneNumberId || '',
          businessAccountId: a.businessAccountId || '',
          status: Number(a.status) || 1,
          configuracoesJson: a.configuracoesJson || '',
        });
        setPossuiAccessToken(Boolean(a.possuiAccessToken));
        setPossuiWebhookSecret(Boolean(a.possuiWebhookSecret));
      } catch (err) {
        setError(getApiErrorMessage(err, 'Erro ao carregar conta.'));
      } finally {
        setPageLoading(false);
      }
    };
    load();
  }, [id, isEditing]);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error('O nome é obrigatório.');
      return;
    }
    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        provider: Number(formData.provider),
        phoneNumberId: formData.phoneNumberId.trim() || null,
        businessAccountId: formData.businessAccountId.trim() || null,
        status: Number(formData.status),
        configuracoesJson: formData.configuracoesJson.trim() || null,
      };
      if (isEditing) {
        // null mantém o valor atual; só envia quando o usuário digitou algo.
        payload.accessToken = accessToken === '' ? null : accessToken;
        payload.webhookSecret = webhookSecret === '' ? null : webhookSecret;
        await whatsappAccountsApi.update(id, payload);
      } else {
        payload.accessToken = accessToken || null;
        payload.webhookSecret = webhookSecret || null;
        await whatsappAccountsApi.create(payload);
      }
      toast.success(isEditing ? 'Conta atualizada.' : 'Conta criada.');
      navigate('/whatsapp/contas');
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao salvar conta.'));
    } finally {
      setLoading(false);
    }
  };

  if (pageLoading) return <LoadingPage text="Carregando conta..." />;
  if (error) return <ErrorPage message={error} onRetry={() => window.location.reload()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/whatsapp/contas"><ArrowLeft className="h-4 w-4 mr-2" /> Voltar</Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? 'Editar conta WhatsApp' : 'Nova conta WhatsApp'}</h1>
          <p className="text-muted-foreground">Credenciais do provedor de envio.</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Dados da conta</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">Nome *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="provider">Provedor</Label>
                <Select value={String(formData.provider)} onValueChange={(v) => setFormData((prev) => ({ ...prev, provider: Number(v) }))}>
                  <SelectTrigger id="provider"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {PROVIDERS.map((p) => <SelectItem key={p.value} value={String(p.value)}>{p.label}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="phoneNumberId">Phone Number ID</Label>
                <Input id="phoneNumberId" name="phoneNumberId" value={formData.phoneNumberId} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="businessAccountId">Business Account ID</Label>
                <Input id="businessAccountId" name="businessAccountId" value={formData.businessAccountId} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <Select value={String(formData.status)} onValueChange={(v) => setFormData((prev) => ({ ...prev, status: Number(v) }))}>
                  <SelectTrigger id="status"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {STATUSES.map((s) => <SelectItem key={s.value} value={String(s.value)}>{s.label}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="accessToken">Access Token</Label>
                  {isEditing && (
                    <StatusBadge tone={possuiAccessToken ? 'success' : 'neutral'}>
                      {possuiAccessToken ? 'Configurado' : 'Não configurado'}
                    </StatusBadge>
                  )}
                </div>
                <Input id="accessToken" type="password" autoComplete="new-password" value={accessToken} onChange={(e) => setAccessToken(e.target.value)} placeholder={isEditing ? 'Deixe em branco para manter' : ''} />
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="webhookSecret">Webhook Secret</Label>
                  {isEditing && (
                    <StatusBadge tone={possuiWebhookSecret ? 'success' : 'neutral'}>
                      {possuiWebhookSecret ? 'Configurado' : 'Não configurado'}
                    </StatusBadge>
                  )}
                </div>
                <Input id="webhookSecret" type="password" autoComplete="new-password" value={webhookSecret} onChange={(e) => setWebhookSecret(e.target.value)} placeholder={isEditing ? 'Deixe em branco para manter' : ''} />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="configuracoesJson">Configurações (JSON)</Label>
              <Input id="configuracoesJson" name="configuracoesJson" value={formData.configuracoesJson} onChange={handleChange} placeholder='{"baseUrl":"..."}' />
            </div>

            <div className="flex items-center space-x-4 pt-2">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? 'Salvando...' : (isEditing ? 'Atualizar' : 'Criar')}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/whatsapp/contas">Cancelar</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
