import React, { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Save, Eye, Clock, MessageSquare } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Switch } from '@/components/ui/switch';
import api from '../../lib/api';
import Loading from '../../components/ui/loading';
import ErrorMessage from '../../components/ui/error-message';
import { useTranslation } from 'react-i18next';

const ConfiguracaoForm = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [formData, setFormData] = useState({
    textoMensagem: '',
    diasAposVisita: 0,
    horarioEnvio: '09:00',
    ativo: true
  });

  useEffect(() => {
    if (isEditing) {
      fetchConfiguracao();
    }
  }, [id, isEditing]);

  const fetchConfiguracao = async () => {
    try {
      setLoading(true);
      const response = await api.get(`/configuracoesMensagens/${id}`);
      const config = response.data;
      
      setFormData({
        textoMensagem: config.textoMensagem || '',
        diasAposVisita: config.diasAposVisita || 0,
        horarioEnvio: config.horarioEnvio ? config.horarioEnvio.substring(0, 5) : '09:00',
        ativo: config.ativo !== undefined ? config.ativo : true
      });
    } catch (err) {
      setError(t('messageSettings.form.errorLoad'));
      console.error('Erro ao buscar configuração:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.textoMensagem.trim()) {
      setError(t('messageSettings.form.validation.messageRequired'));
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const payload = {
        textoMensagem: formData.textoMensagem.trim(),
        diasAposVisita: parseInt(formData.diasAposVisita),
        horarioEnvio: formData.horarioEnvio + ':00', // Adiciona segundos
        ativo: formData.ativo
      };

      if (isEditing) {
        await api.put(`/configuracoesMensagens/${id}`, payload);
      } else {
        await api.post('/configuracoesMensagens', payload);
      }

      navigate('/configuracoes-mensagens');
    } catch (err) {
      setError(isEditing ? t('messageSettings.form.updateError') : t('messageSettings.form.createError'));
      console.error('Erro ao salvar configuração:', err);
    } finally {
      setLoading(false);
    }
  };

  const getPreviewMessage = () => {
    if (!formData.textoMensagem) return '';
    
    // Substitui variáveis por valores de exemplo
    return formData.textoMensagem
      .replace(/{Nome}/g, t('messageSettings.form.previewExampleName'))
      .replace(/{nome}/g, t('messageSettings.form.previewExampleName'));
  };

  const getDiasText = () => {
    if (Number(formData.diasAposVisita) === 0) return t('messageSettings.form.schedule.sameDay');
    if (Number(formData.diasAposVisita) === 1) return t('messageSettings.form.schedule.oneDayAfter');
    return t('messageSettings.form.schedule.daysAfter', { count: formData.diasAposVisita });
  };

  if (loading && isEditing) return <Loading />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/configuracoes-mensagens')}
        >
          <ArrowLeft className="w-5 h-5" />
        </Button>
        <div>
          <h1 className="text-3xl font-bold text-foreground">
            {isEditing ? t('messageSettings.form.editTitle') : t('messageSettings.form.newTitle')}
          </h1>
          <p className="text-muted-foreground mt-1">
            {isEditing ? t('messageSettings.form.editSubtitle') : t('messageSettings.form.createSubtitle')}
          </p>
        </div>
      </div>

      {error && <ErrorMessage message={error} />}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Formulário */}
        <Card>
          <CardContent className="p-6">
            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Texto da Mensagem */}
              <div className="space-y-2">
                <Label htmlFor="textoMensagem">
                  {t('messageSettings.form.fields.messageText')} *
                </Label>
                <Textarea
                  id="textoMensagem"
                  name="textoMensagem"
                  value={formData.textoMensagem}
                  onChange={handleInputChange}
                  rows={6}
                  placeholder={t('messageSettings.form.placeholders.messageText')}
                  required
                />
                <p className="text-xs text-muted-foreground">
                  {t('messageSettings.form.messageTip')}
                </p>
              </div>

              {/* Dias Após Visita */}
              <div className="space-y-2">
                <Label htmlFor="diasAposVisita">
                  {t('messageSettings.form.fields.daysAfterVisit')}
                </Label>
                <select
                  id="diasAposVisita"
                  name="diasAposVisita"
                  value={formData.diasAposVisita}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 bg-background border border-input rounded-lg focus:ring-2 focus:ring-ring focus:border-ring"
                >
                  <option value={0}>{t('messageSettings.form.scheduleOptions.sameDay')}</option>
                  <option value={1}>{t('messageSettings.form.scheduleOptions.day1')}</option>
                  <option value={2}>{t('messageSettings.form.scheduleOptions.day2')}</option>
                  <option value={3}>{t('messageSettings.form.scheduleOptions.day3')}</option>
                  <option value={7}>{t('messageSettings.form.scheduleOptions.week1')}</option>
                  <option value={14}>{t('messageSettings.form.scheduleOptions.week2')}</option>
                  <option value={30}>{t('messageSettings.form.scheduleOptions.month1')}</option>
                </select>
              </div>

              {/* Horário de Envio */}
              <div className="space-y-2">
                <Label htmlFor="horarioEnvio">
                  {t('messageSettings.form.fields.sendTime')}
                </Label>
                <Input
                  type="time"
                  id="horarioEnvio"
                  name="horarioEnvio"
                  value={formData.horarioEnvio}
                  onChange={handleInputChange}
                  required
                />
              </div>

              {/* Status Ativo */}
              <div className="flex items-center space-x-2">
                <Switch
                  id="ativo"
                  name="ativo"
                  checked={formData.ativo}
                  onCheckedChange={(checked) => setFormData(prev => ({ ...prev, ativo: checked }))}
                />
                <Label htmlFor="ativo" className="cursor-pointer">
                  {t('messageSettings.form.fields.active')}
                </Label>
              </div>

              {/* Botões */}
              <div className="flex justify-end space-x-3 pt-6 border-t border-border">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/configuracoes-mensagens')}
                >
                  {t('actions.cancel')}
                </Button>
                <Button
                  type="submit"
                  disabled={loading}
                >
                  <Save className="w-4 h-4 mr-2" />
                  {loading ? t('actions.saving') : (isEditing ? t('actions.update') : t('actions.create'))}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>

        {/* Preview */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Eye className="w-5 h-5" />
              {t('messageSettings.form.previewTitle')}
            </CardTitle>
          </CardHeader>
          <CardContent>
            {/* Informações de Agendamento */}
            <div className="bg-blue-500/10 dark:bg-blue-500/20 rounded-lg p-4 mb-4 border border-blue-500/20">
              <div className="flex items-center space-x-2 text-blue-600 dark:text-blue-400 mb-2">
                <Clock className="w-4 h-4" />
                <span className="font-medium">{t('messageSettings.form.scheduleTitle')}</span>
              </div>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                {t('messageSettings.form.scheduleDescription', { period: getDiasText(), time: formData.horarioEnvio })}
              </p>
            </div>

            {/* Preview da Mensagem */}
            <div className="border border-border rounded-lg p-4">
              <div className="flex items-center space-x-2 mb-3">
                <MessageSquare className="w-4 h-4 text-green-500 dark:text-green-400" />
                <span className="text-sm font-medium text-foreground">{t('messageSettings.form.previewChannel')}</span>
              </div>
            
              {formData.textoMensagem ? (
                <div className="bg-green-500/10 dark:bg-green-500/20 rounded-lg p-3 max-w-xs border border-green-500/20">
                  <p className="text-sm text-foreground whitespace-pre-wrap">
                    {getPreviewMessage()}
                  </p>
                </div>
              ) : (
                <div className="text-muted-foreground text-sm italic">
                  Digite o texto da mensagem para ver o preview
                </div>
              )}
            </div>

            {/* Status */}
            <div className="mt-4 pt-4 border-t border-border">
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">Status:</span>
                {formData.ativo ? (
                  <Badge variant="default" className="bg-green-500 hover:bg-green-600 dark:bg-green-600 dark:hover:bg-green-700">
                    Ativa
                  </Badge>
                ) : (
                  <Badge variant="secondary">Inativa</Badge>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default ConfiguracaoForm;
