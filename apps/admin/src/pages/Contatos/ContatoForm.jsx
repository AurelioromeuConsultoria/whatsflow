import { useEffect, useState } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { ArrowLeft, Save } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { LoadingPage } from '@/components/ui/loading';
import { ErrorPage } from '@/components/ui/error-message';
import { contatosApi, tagsApi } from '@/lib/api';
import { toast } from 'sonner';
import { getApiErrorMessage } from '@/lib/apiError';

const EMPTY = {
  nome: '',
  telefoneWhatsApp: '',
  email: '',
  documento: '',
  organizacao: '',
  observacoes: '',
  origem: '',
  status: 1,
  optIn: false,
  tagIds: [],
};

export default function ContatoForm() {
  const navigate = useNavigate();
  const { id } = useParams();
  const isEditing = Boolean(id);

  const [formData, setFormData] = useState(EMPTY);
  const [tags, setTags] = useState([]);
  const [loading, setLoading] = useState(false);
  const [pageLoading, setPageLoading] = useState(isEditing);
  const [error, setError] = useState(null);

  useEffect(() => {
    tagsApi.getAll().then((r) => setTags(r.data || [])).catch(() => setTags([]));
  }, []);

  useEffect(() => {
    if (!isEditing) return;
    const load = async () => {
      try {
        setPageLoading(true);
        setError(null);
        const res = await contatosApi.getById(id);
        const c = res.data;
        setFormData({
          nome: c.nome || '',
          telefoneWhatsApp: c.telefoneWhatsApp || '',
          email: c.email || '',
          documento: c.documento || '',
          organizacao: c.organizacao || '',
          observacoes: c.observacoes || '',
          origem: c.origem || '',
          status: Number(c.status) || 1,
          optIn: Boolean(c.optIn),
          tagIds: (c.tags || []).map((tag) => tag.id),
        });
      } catch (err) {
        setError(getApiErrorMessage(err, 'Erro ao carregar contato.'));
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

  const toggleTag = (tagId) => {
    setFormData((prev) => ({
      ...prev,
      tagIds: prev.tagIds.includes(tagId)
        ? prev.tagIds.filter((tid) => tid !== tagId)
        : [...prev.tagIds, tagId],
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.nome.trim()) {
      toast.error('O nome é obrigatório.');
      return;
    }
    if (!formData.telefoneWhatsApp.trim()) {
      toast.error('O telefone do WhatsApp é obrigatório.');
      return;
    }
    try {
      setLoading(true);
      const payload = {
        nome: formData.nome.trim(),
        telefoneWhatsApp: formData.telefoneWhatsApp.trim(),
        email: formData.email.trim() || null,
        documento: formData.documento.trim() || null,
        organizacao: formData.organizacao.trim() || null,
        observacoes: formData.observacoes.trim() || null,
        origem: formData.origem.trim() || null,
        status: Number(formData.status),
        optIn: formData.optIn,
        tagIds: formData.tagIds,
      };
      if (isEditing) await contatosApi.update(id, payload);
      else await contatosApi.create(payload);
      toast.success(isEditing ? 'Contato atualizado.' : 'Contato criado.');
      navigate('/contatos');
    } catch (err) {
      toast.error(getApiErrorMessage(err, 'Erro ao salvar contato.'));
    } finally {
      setLoading(false);
    }
  };

  if (pageLoading) return <LoadingPage text="Carregando contato..." />;
  if (error) return <ErrorPage message={error} onRetry={() => window.location.reload()} />;

  return (
    <div className="space-y-6">
      <div className="flex items-center space-x-4">
        <Button variant="ghost" asChild>
          <Link to="/contatos"><ArrowLeft className="h-4 w-4 mr-2" /> Voltar</Link>
        </Button>
        <div>
          <h1 className="text-3xl font-bold">{isEditing ? 'Editar contato' : 'Novo contato'}</h1>
          <p className="text-muted-foreground">Dados do contato e canais de comunicação.</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Dados do contato</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="nome">Nome *</Label>
                <Input id="nome" name="nome" value={formData.nome} onChange={handleChange} required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="telefoneWhatsApp">Telefone WhatsApp *</Label>
                <Input id="telefoneWhatsApp" name="telefoneWhatsApp" value={formData.telefoneWhatsApp} onChange={handleChange} placeholder="5511999999999" required />
              </div>
              <div className="space-y-2">
                <Label htmlFor="email">E-mail</Label>
                <Input id="email" name="email" type="email" value={formData.email} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="documento">Documento</Label>
                <Input id="documento" name="documento" value={formData.documento} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="organizacao">Organização</Label>
                <Input id="organizacao" name="organizacao" value={formData.organizacao} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="origem">Origem</Label>
                <Input id="origem" name="origem" value={formData.origem} onChange={handleChange} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="status">Status</Label>
                <Select value={String(formData.status)} onValueChange={(v) => setFormData((prev) => ({ ...prev, status: Number(v) }))}>
                  <SelectTrigger id="status"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="1">Ativo</SelectItem>
                    <SelectItem value="2">Inativo</SelectItem>
                    <SelectItem value="3">Bloqueado</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="flex items-center space-x-3 pt-8">
                <Switch id="optIn" checked={formData.optIn} onCheckedChange={(checked) => setFormData((prev) => ({ ...prev, optIn: checked }))} />
                <Label htmlFor="optIn" className="cursor-pointer">Opt-in (autorizou contato)</Label>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="observacoes">Observações</Label>
              <Textarea id="observacoes" name="observacoes" value={formData.observacoes} onChange={handleChange} rows={3} />
            </div>

            <div className="space-y-2">
              <Label>Tags</Label>
              {tags.length === 0 ? (
                <p className="text-sm text-muted-foreground">Nenhuma tag cadastrada. <Link to="/tags/novo" className="text-primary underline">Criar tag</Link></p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {tags.map((tag) => {
                    const selected = formData.tagIds.includes(tag.id);
                    return (
                      <button key={tag.id} type="button" onClick={() => toggleTag(tag.id)}>
                        <Badge
                          variant={selected ? 'default' : 'outline'}
                          className="cursor-pointer"
                          style={selected && tag.cor ? { backgroundColor: tag.cor, color: '#fff', borderColor: tag.cor } : undefined}
                        >
                          {tag.nome}
                        </Badge>
                      </button>
                    );
                  })}
                </div>
              )}
            </div>

            <div className="flex items-center space-x-4 pt-2">
              <Button type="submit" disabled={loading}>
                <Save className="h-4 w-4 mr-2" /> {loading ? 'Salvando...' : (isEditing ? 'Atualizar' : 'Criar')}
              </Button>
              <Button type="button" variant="outline" asChild>
                <Link to="/contatos">Cancelar</Link>
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
