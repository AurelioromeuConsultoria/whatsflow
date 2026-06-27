import { useEffect, useState } from 'react';
import { Save, ShieldCheck } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { doacoesConfigApi } from '@/lib/api';

export default function DoacoesConfigAsaas() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [config, setConfig] = useState(null);
  const [formData, setFormData] = useState({
    environment: '1',
    apiKey: '',
    webhookUrl: '',
    webhookSecret: '',
    pixEnabled: true,
    creditCardEnabled: false,
    boletoEnabled: false,
    ativo: false,
  });

  const load = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await doacoesConfigApi.getAsaas();
      const item = res.data || {};
      setConfig(item);
      setFormData({
        environment: String(item.environment || 1),
        apiKey: '',
        webhookUrl: item.webhookUrl || '',
        webhookSecret: '',
        pixEnabled: item.pixEnabled ?? true,
        creditCardEnabled: item.creditCardEnabled ?? false,
        boletoEnabled: item.boletoEnabled ?? false,
        ativo: item.ativo ?? false,
      });
    } catch (err) {
      setError('Não foi possível carregar a configuração Asaas.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;
    setFormData((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (formData.ativo && !config?.configurado && !formData.apiKey.trim()) {
      toast.error('Informe a API Key antes de ativar a integração.');
      return;
    }

    try {
      setSaving(true);
      const payload = {
        environment: Number(formData.environment),
        apiKey: formData.apiKey.trim() || null,
        webhookUrl: formData.webhookUrl.trim() || null,
        webhookSecret: formData.webhookSecret.trim() || null,
        pixEnabled: formData.pixEnabled,
        creditCardEnabled: formData.creditCardEnabled,
        boletoEnabled: formData.boletoEnabled,
        ativo: formData.ativo,
      };
      const res = await doacoesConfigApi.saveAsaas(payload);
      setConfig(res.data);
      setFormData((current) => ({ ...current, apiKey: '', webhookSecret: '' }));
      toast.success('Configuração Asaas salva.');
    } catch (err) {
      toast.error(err.response?.data?.message || 'Não foi possível salvar a configuração.');
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingPage text="Carregando configuração Asaas..." />;
  if (error) return <ErrorPage message={error} onRetry={load} />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Configuração Asaas</h1>
        <p className="text-muted-foreground">Conecte a conta Asaas desta igreja para gerar Pix e cartão nas doações online.</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5" />
            Status da integração
          </CardTitle>
        </CardHeader>
        <CardContent className="flex flex-wrap gap-2">
          <Badge variant={config?.configurado ? 'default' : 'outline'}>
            {config?.configurado ? `API Key ${config.apiKeyUltimosDigitos || 'configurada'}` : 'Sem API Key'}
          </Badge>
          <Badge variant={config?.ativo ? 'default' : 'outline'}>{config?.ativo ? 'Ativa' : 'Inativa'}</Badge>
          <Badge variant="secondary">{config?.environmentDescricao || 'Sandbox'}</Badge>
        </CardContent>
      </Card>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Credenciais</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Ambiente</Label>
              <Select value={formData.environment} onValueChange={(value) => setFormData((current) => ({ ...current, environment: value }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="1">Sandbox</SelectItem>
                  <SelectItem value="2">Produção</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="apiKey">API Key Asaas</Label>
              <Input id="apiKey" name="apiKey" type="password" value={formData.apiKey} onChange={handleChange} placeholder={config?.configurado ? 'Preencha apenas para substituir' : '$aact_YOUR_API_KEY'} />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="webhookUrl">Webhook URL</Label>
              <Input id="webhookUrl" name="webhookUrl" value={formData.webhookUrl} onChange={handleChange} placeholder="https://api.seudominio.com/api/webhooks/asaas" />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label htmlFor="webhookSecret">Webhook secret</Label>
              <Input id="webhookSecret" name="webhookSecret" type="password" value={formData.webhookSecret} onChange={handleChange} placeholder="Preencha apenas para definir/substituir" />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Métodos e ativação</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-wrap gap-5">
            {[
              ['pixEnabled', 'Pix'],
              ['creditCardEnabled', 'Cartão de crédito'],
              ['boletoEnabled', 'Boleto'],
              ['ativo', 'Ativar integração'],
            ].map(([name, label]) => (
              <label key={name} className="flex items-center gap-2 text-sm font-medium">
                <input type="checkbox" name={name} checked={formData[name]} onChange={handleChange} className="h-4 w-4" />
                {label}
              </label>
            ))}
          </CardContent>
        </Card>

        <Button type="submit" disabled={saving}>
          <Save className="mr-2 h-4 w-4" />
          {saving ? 'Salvando...' : 'Salvar configuração'}
        </Button>
      </form>
    </div>
  );
}
