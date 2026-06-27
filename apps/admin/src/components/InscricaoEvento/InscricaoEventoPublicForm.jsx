import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { inscricoesEventosApi } from '@/lib/api';
import { toast } from 'sonner';
import { useTranslation } from 'react-i18next';

export default function InscricaoEventoPublicForm({ eventoId, onSuccess, onCancel }) {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    nome: '',
    whatsApp: '',
    email: '',
    quantidadeAcompanhantes: 0,
    observacoes: '',
  });
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: name === 'quantidadeAcompanhantes' ? Number(value) : value }));
  };

  const formatWhatsApp = (value) => {
    const numbers = value.replace(/\D/g, '');
    if (numbers.length <= 11) {
      if (numbers.length <= 2) return numbers;
      if (numbers.length <= 7) return `(${numbers.slice(0, 2)}) ${numbers.slice(2)}`;
      return `(${numbers.slice(0, 2)}) ${numbers.slice(2, 7)}-${numbers.slice(7)}`;
    }
    return value;
  };

  const handleWhatsAppChange = (e) => {
    const formatted = formatWhatsApp(e.target.value);
    setFormData((prev) => ({ ...prev, whatsApp: formatted }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!formData.nome.trim()) {
      toast.error(t('eventRegistrations.publicForm.validation.nameRequired'));
      return;
    }

    if (!formData.whatsApp.trim()) {
      toast.error(t('eventRegistrations.publicForm.validation.whatsAppRequired'));
      return;
    }

    const whatsAppNumbers = formData.whatsApp.replace(/\D/g, '');
    if (whatsAppNumbers.length < 10 || whatsAppNumbers.length > 13) {
      toast.error(t('eventRegistrations.publicForm.validation.whatsAppInvalid'));
      return;
    }

    if (formData.email && !/.+@.+\..+/.test(formData.email)) {
      toast.error(t('eventRegistrations.publicForm.validation.emailInvalid'));
      return;
    }

    try {
      setLoading(true);
      const payload = {
        eventoId: Number(eventoId),
        nome: formData.nome.trim(),
        whatsApp: whatsAppNumbers,
        email: formData.email?.trim() || null,
        quantidadeAcompanhantes: formData.quantidadeAcompanhantes || 0,
        observacoes: formData.observacoes?.trim() || null,
      };

      await inscricoesEventosApi.create(payload);
      toast.success(t('eventRegistrations.publicForm.success'));
      
      if (onSuccess) {
        onSuccess();
      }
    } catch (err) {
      let errorMessage = t('eventRegistrations.publicForm.errorDefault');
      if (err.response?.data?.message) {
        const msg = err.response.data.message;
        if (msg.includes('já iniciou')) {
          errorMessage = t('eventRegistrations.publicForm.errors.alreadyStarted');
        } else if (msg.includes('já existe') || msg.includes('duplicada')) {
          errorMessage = t('eventRegistrations.publicForm.errors.duplicate');
        } else if (msg.includes('não encontrado')) {
          errorMessage = t('eventRegistrations.publicForm.errors.notFound');
        } else {
          errorMessage = msg;
        }
      }
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('eventRegistrations.publicForm.title')}</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="nome">{t('eventRegistrations.publicForm.fields.name')} *</Label>
            <Input
              id="nome"
              name="nome"
              value={formData.nome}
              onChange={handleChange}
              placeholder={t('eventRegistrations.publicForm.placeholders.name')}
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="whatsApp">{t('eventRegistrations.publicForm.fields.whatsApp')} *</Label>
            <Input
              id="whatsApp"
              name="whatsApp"
              value={formData.whatsApp}
              onChange={handleWhatsAppChange}
              placeholder="(11) 99999-9999"
              required
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">{t('eventRegistrations.publicForm.fields.email')}</Label>
            <Input
              id="email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleChange}
              placeholder={t('eventRegistrations.publicForm.placeholders.email')}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="quantidadeAcompanhantes">{t('eventRegistrations.publicForm.fields.companions')}</Label>
            <Input
              id="quantidadeAcompanhantes"
              name="quantidadeAcompanhantes"
              type="number"
              min="0"
              value={formData.quantidadeAcompanhantes}
              onChange={handleChange}
              placeholder="0"
            />
            <p className="text-xs text-muted-foreground">{t('eventRegistrations.publicForm.fields.companionsHint')}</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="observacoes">{t('eventRegistrations.publicForm.fields.notes')}</Label>
            <Textarea
              id="observacoes"
              name="observacoes"
              value={formData.observacoes}
              onChange={handleChange}
              placeholder={t('eventRegistrations.publicForm.placeholders.notes')}
              rows={3}
            />
          </div>

          <div className="flex items-center space-x-4">
            <Button type="submit" disabled={loading}>
              {loading ? t('eventRegistrations.publicForm.submitting') : t('eventRegistrations.publicForm.submit')}
            </Button>
            {onCancel && (
              <Button type="button" variant="outline" onClick={onCancel}>
                {t('actions.cancel')}
              </Button>
            )}
          </div>
        </form>
      </CardContent>
    </Card>
  );
}






