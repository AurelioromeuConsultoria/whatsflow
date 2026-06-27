import React from 'react';
import { CheckCircle2, PhoneCall } from 'lucide-react';
import Loading from '../../../components/ui/loading';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
import { ImageUpload } from '@/components/ImageUpload';
import { useTranslation } from 'react-i18next';
import { EstadoVazio } from './KidsShared';
import { OCORRENCIA_TIPOS, formatOcorrenciaTipo, getOcorrenciaStatusConfig, isOcorrenciaEncerrada } from './kidsHelpers';

export function CriancaDialog({
  open,
  onOpenChange,
  form,
  onChange,
  onSave,
  saving,
  salas,
  turmas,
}) {
  const { t } = useTranslation();
  const turmasDaSala = form.salaId ? turmas.filter((item) => item.salaId === form.salaId) : [];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('kids.children.new')}</DialogTitle>
          <DialogDescription>
            {t('kids.children.dialogDescription')}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="criancaNome">{t('kids.common.name')}</Label>
              <Input id="criancaNome" value={form.nome} onChange={(e) => onChange('nome', e.target.value)} placeholder={t('kids.children.namePlaceholder')} maxLength={100} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="criancaNascimento">{t('kids.children.birthDate')}</Label>
              <Input id="criancaNascimento" type="date" value={form.dataNascimento} onChange={(e) => onChange('dataNascimento', e.target.value)} />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="criancaSala">{t('kids.common.room')}</Label>
              <Select value={form.salaId || 'selecionar'} onValueChange={(value) => onChange('salaId', value === 'selecionar' ? '' : value)}>
                <SelectTrigger id="criancaSala">
                  <SelectValue placeholder={t('kids.common.selectRoom')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="selecionar">{t('kids.common.selectRoom')}</SelectItem>
                  {salas.map((sala) => (
                    <SelectItem key={sala.id} value={sala.id}>
                      {sala.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="criancaTurma">{t('kids.common.class')}</Label>
              <Select value={form.turmaId || 'selecionar'} onValueChange={(value) => onChange('turmaId', value === 'selecionar' ? '' : value)}>
                <SelectTrigger id="criancaTurma">
                  <SelectValue placeholder={t('kids.common.selectClass')} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="selecionar">{t('kids.common.selectClass')}</SelectItem>
                  {turmasDaSala.map((turma) => (
                    <SelectItem key={turma.id} value={turma.id}>
                      {turma.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="criancaAlergias">{t('kids.children.allergies')}</Label>
              <Textarea id="criancaAlergias" value={form.alergias} onChange={(e) => onChange('alergias', e.target.value)} rows={3} maxLength={500} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="criancaRestricoes">{t('kids.children.foodRestrictions')}</Label>
              <Textarea id="criancaRestricoes" value={form.restricoesAlimentares} onChange={(e) => onChange('restricoesAlimentares', e.target.value)} rows={3} maxLength={500} />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="criancaObservacoes">{t('kids.common.notes')}</Label>
            <Textarea id="criancaObservacoes" value={form.observacoes} onChange={(e) => onChange('observacoes', e.target.value)} rows={4} maxLength={1000} />
          </div>

          <label htmlFor="criancaConsentimento" className="flex items-start gap-2 text-sm">
            <input
              id="criancaConsentimento"
              type="checkbox"
              className="mt-1"
              checked={!!form.consentimentoParental}
              onChange={(e) => onChange('consentimentoParental', e.target.checked)}
            />
            <span>
              {t('kids.children.parentalConsent', {
                defaultValue: 'Confirmo que obtive o consentimento parental do responsável para o tratamento dos dados desta criança (LGPD).',
              })}
            </span>
          </label>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving}>
            {saving ? t('actions.saving') : t('kids.children.create')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function ConteudoAulaDialog({
  open,
  onOpenChange,
  form,
  onChange,
  onAnexoChange,
  onAddAnexo,
  onRemoveAnexo,
  onSave,
  saving,
  salas,
  turmas,
  isEditing,
}) {
  const { t } = useTranslation();
  const turmasDaSala = form.salaId ? turmas.filter((item) => item.salaId === form.salaId) : [];

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>
            {isEditing
              ? t('kids.lessonContent.editTitle', { defaultValue: 'Editar conteúdo da aula' })
              : t('kids.lessonContent.createTitle', { defaultValue: 'Novo conteúdo da aula' })}
          </DialogTitle>
          <DialogDescription>
            {t('kids.lessonContent.description', {
              defaultValue: 'Publique resumo, versículo, atividade em casa e materiais para os responsáveis no AppKids.',
            })}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2 max-h-[72vh] overflow-y-auto pr-1">
          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2 md:col-span-2">
              <Label htmlFor="conteudoTitulo">{t('kids.lessonContent.title', { defaultValue: 'Título' })}</Label>
              <Input id="conteudoTitulo" value={form.titulo} onChange={(e) => onChange('titulo', e.target.value)} maxLength={200} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="conteudoTema">{t('kids.lessonContent.theme', { defaultValue: 'Tema' })}</Label>
              <Input id="conteudoTema" value={form.tema} onChange={(e) => onChange('tema', e.target.value)} maxLength={200} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="conteudoVersiculo">{t('kids.lessonContent.verse', { defaultValue: 'Versículo' })}</Label>
              <Input id="conteudoVersiculo" value={form.versiculo} onChange={(e) => onChange('versiculo', e.target.value)} maxLength={300} />
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="grid gap-2">
              <Label htmlFor="conteudoData">{t('kids.lessonContent.referenceDate', { defaultValue: 'Data de referência' })}</Label>
              <Input id="conteudoData" type="date" value={form.dataReferencia} onChange={(e) => onChange('dataReferencia', e.target.value)} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="conteudoSala">{t('kids.common.room')}</Label>
              <Select value={form.salaId || 'todas'} onValueChange={(value) => onChange('salaId', value === 'todas' ? '' : value)}>
                <SelectTrigger id="conteudoSala">
                  <SelectValue placeholder={t('kids.lessonContent.allRooms', { defaultValue: 'Todas as salas' })} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todas">{t('kids.lessonContent.allRooms', { defaultValue: 'Todas as salas' })}</SelectItem>
                  {salas.map((sala) => (
                    <SelectItem key={sala.id} value={sala.id}>
                      {sala.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="conteudoTurma">{t('kids.common.class')}</Label>
              <Select value={form.turmaId || 'todas'} onValueChange={(value) => onChange('turmaId', value === 'todas' ? '' : value)}>
                <SelectTrigger id="conteudoTurma">
                  <SelectValue placeholder={t('kids.lessonContent.allClasses', { defaultValue: 'Todas as turmas' })} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="todas">{t('kids.lessonContent.allClasses', { defaultValue: 'Todas as turmas' })}</SelectItem>
                  {turmasDaSala.map((turma) => (
                    <SelectItem key={turma.id} value={turma.id}>
                      {turma.nome}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="conteudoResumo">{t('kids.lessonContent.summary', { defaultValue: 'Resumo da aula' })}</Label>
            <Textarea id="conteudoResumo" value={form.resumo} onChange={(e) => onChange('resumo', e.target.value)} rows={4} maxLength={4000} />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="conteudoAtividade">{t('kids.lessonContent.homeActivity', { defaultValue: 'Atividade em casa' })}</Label>
            <Textarea id="conteudoAtividade" value={form.atividadeEmCasa} onChange={(e) => onChange('atividadeEmCasa', e.target.value)} rows={3} maxLength={2000} />
          </div>

          <div className="grid gap-2">
            <Label htmlFor="conteudoObservacao">{t('kids.lessonContent.familyNote', { defaultValue: 'Recado para a família' })}</Label>
            <Textarea id="conteudoObservacao" value={form.observacaoResponsavel} onChange={(e) => onChange('observacaoResponsavel', e.target.value)} rows={3} maxLength={1000} />
          </div>

          <div className="grid gap-3">
            <div className="flex items-center justify-between gap-3">
              <div>
                <Label>{t('kids.lessonContent.attachments', { defaultValue: 'Materiais anexos' })}</Label>
                <p className="text-sm text-muted-foreground">
                  {t('kids.lessonContent.attachmentsHint', {
                    defaultValue: 'Envie PDF/imagem ou informe um link externo para compartilhar com as famílias.',
                  })}
                </p>
              </div>
              <Button type="button" variant="outline" onClick={onAddAnexo}>
                {t('kids.lessonContent.addAttachment', { defaultValue: 'Adicionar material' })}
              </Button>
            </div>

            {form.anexos.length ? (
              <div className="space-y-3">
                {form.anexos.map((anexo, index) => (
                  <div key={`anexo-${index}`} className="rounded-xl border border-border p-4">
                    <div className="grid gap-4 md:grid-cols-2">
                      <div className="grid gap-2">
                        <Label>{t('kids.lessonContent.attachmentType', { defaultValue: 'Tipo' })}</Label>
                        <Select value={anexo.tipo || 'Pdf'} onValueChange={(value) => onAnexoChange(index, 'tipo', value)}>
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Pdf">PDF</SelectItem>
                            <SelectItem value="Imagem">Imagem</SelectItem>
                            <SelectItem value="Link">Link</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="grid gap-2">
                        <Label>{t('kids.lessonContent.attachmentName', { defaultValue: 'Nome de exibição' })}</Label>
                        <Input value={anexo.nomeExibicao} onChange={(e) => onAnexoChange(index, 'nomeExibicao', e.target.value)} maxLength={200} />
                      </div>
                      <div className="grid gap-2 md:col-span-2">
                        <Label>
                          {anexo.tipo === 'Link'
                            ? t('kids.lessonContent.attachmentReference', { defaultValue: 'Link externo' })
                            : t('kids.lessonContent.attachmentUpload', { defaultValue: 'Arquivo do material' })}
                        </Label>
                        {anexo.tipo === 'Link' ? (
                          <Input value={anexo.url} onChange={(e) => onAnexoChange(index, 'url', e.target.value)} maxLength={1000} />
                        ) : (
                          <ImageUpload
                            value={anexo.url}
                            onChange={(value) => onAnexoChange(index, 'url', value)}
                            label=""
                            type={anexo.tipo === 'Imagem' ? 'image' : 'file'}
                            accept={anexo.tipo === 'Imagem' ? 'image/*' : '.pdf'}
                          />
                        )}
                      </div>
                    </div>
                    <div className="mt-3 flex justify-end">
                      <Button type="button" variant="ghost" onClick={() => onRemoveAnexo(index)}>
                        {t('actions.remove', { defaultValue: 'Remover' })}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <EstadoVazio
                texto={t('kids.lessonContent.attachmentsEmpty', {
                  defaultValue: 'Nenhum material adicionado ainda.',
                })}
              />
            )}
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving}>
            {saving ? t('actions.saving') : t('actions.save')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function ResponsavelDialog({
  open,
  onOpenChange,
  crianca,
  query,
  onQueryChange,
  onBuscar,
  searching,
  resultados,
  onSelecionarPessoa,
  pessoaSelecionada,
  form,
  onChange,
  onSave,
  saving,
  onDesvincular,
  desvinculandoId,
}) {
  const { t } = useTranslation();

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('kids.children.linkGuardianTitle', { defaultValue: 'Vincular responsável' })}</DialogTitle>
          <DialogDescription>
            {crianca
              ? t('kids.children.linkGuardianDescription', {
                  defaultValue: 'Defina quem pode acompanhar e retirar {{name}} no AppKids.',
                  name: crianca.nome,
                })
              : t('kids.children.linkGuardianFallback', { defaultValue: 'Selecione um responsável para a criança.' })}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-3 md:grid-cols-[1fr_auto] md:items-end">
            <div className="grid gap-2">
              <Label htmlFor="buscarResponsavel">{t('kids.children.guardianSearchLabel', { defaultValue: 'Buscar pessoa' })}</Label>
              <Input
                id="buscarResponsavel"
                value={query}
                onChange={(e) => onQueryChange(e.target.value)}
                placeholder={t('kids.children.guardianSearchPlaceholder', {
                  defaultValue: 'Nome, e-mail, telefone ou WhatsApp',
                })}
              />
            </div>
            <Button variant="outline" onClick={onBuscar} disabled={searching}>
              {searching
                ? t('kids.children.searchingGuardian', { defaultValue: 'Buscando...' })
                : t('kids.children.searchGuardian', { defaultValue: 'Buscar' })}
            </Button>
          </div>

          {!!resultados.length && (
            <div className="rounded-lg border border-border divide-y divide-border overflow-hidden">
              {resultados.map((pessoa) => (
                <button
                  key={pessoa.id}
                  type="button"
                  onClick={() => onSelecionarPessoa(pessoa)}
                  className="w-full text-left px-4 py-3 hover:bg-muted/50 transition-colors"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <div className="font-medium text-foreground">{pessoa.nome}</div>
                      <div className="text-sm text-muted-foreground">
                        {[pessoa.email, pessoa.whatsApp || pessoa.telefone].filter(Boolean).join(' • ')
                          || t('kids.children.noGuardianContact', { defaultValue: 'Sem contato principal' })}
                      </div>
                    </div>
                    <Badge variant="outline">
                      {t('kids.children.personIdBadge', { defaultValue: 'Pessoa #{{id}}', id: pessoa.id })}
                    </Badge>
                  </div>
                </button>
              ))}
            </div>
          )}

          {pessoaSelecionada && (
            <div className="rounded-lg border border-border bg-muted/30 px-4 py-3">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <div className="font-medium text-foreground">
                    {t('kids.children.selectedGuardian', { defaultValue: 'Responsável selecionado' })}
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {[pessoaSelecionada.nome, pessoaSelecionada.email, pessoaSelecionada.whatsApp || pessoaSelecionada.telefone]
                      .filter(Boolean)
                      .join(' • ')}
                  </div>
                </div>
                <Badge variant="secondary">
                  {t('kids.children.personIdBadge', { defaultValue: 'Pessoa #{{id}}', id: pessoaSelecionada.id })}
                </Badge>
              </div>
            </div>
          )}

          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="parentescoResponsavel">{t('kids.children.guardianRelationship', { defaultValue: 'Parentesco' })}</Label>
              <Input
                id="parentescoResponsavel"
                value={form.parentesco}
                onChange={(e) => onChange('parentesco', e.target.value)}
                placeholder={t('kids.children.guardianRelationshipPlaceholder', { defaultValue: 'Ex.: Mãe, Pai, Avó' })}
                maxLength={50}
              />
            </div>

            <div className="flex items-center justify-between gap-3 rounded-xl border border-border p-4">
              <div>
                <p className="text-sm font-medium text-foreground">
                  {t('kids.children.guardianCanPickup', { defaultValue: 'Pode retirar' })}
                </p>
                <p className="text-sm text-muted-foreground">
                  {t('kids.children.guardianCanPickupHint', { defaultValue: 'Permite usar este vínculo na retirada da criança.' })}
                </p>
              </div>
              <Switch checked={form.podeRetirar} onCheckedChange={(checked) => onChange('podeRetirar', checked)} />
            </div>
          </div>

          {crianca?.responsaveis?.length ? (
            <div className="grid gap-2">
              <Label>{t('kids.children.currentGuardians', { defaultValue: 'Responsáveis atuais' })}</Label>
              <div className="space-y-2">
                {crianca.responsaveis.map((responsavel) => (
                  <div
                    key={responsavel.id}
                    className="flex flex-col gap-2 rounded-lg border border-border bg-background px-4 py-3 md:flex-row md:items-center md:justify-between"
                  >
                    <div>
                      <div className="font-medium text-foreground">{responsavel.responsavelNome}</div>
                      <div className="text-sm text-muted-foreground">
                        {[responsavel.parentesco, responsavel.responsavelEmail, responsavel.responsavelWhatsApp || responsavel.responsavelTelefone]
                          .filter(Boolean)
                          .join(' • ')}
                      </div>
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge variant={responsavel.podeRetirar ? 'default' : 'outline'}>
                        {responsavel.podeRetirar
                          ? t('kids.children.guardianPickupAllowed', { defaultValue: 'Pode retirar' })
                          : t('kids.children.guardianPickupBlocked', { defaultValue: 'Sem retirada' })}
                      </Badge>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => onDesvincular(responsavel)}
                        disabled={desvinculandoId === responsavel.id}
                      >
                        {desvinculandoId === responsavel.id
                          ? t('actions.removing', { defaultValue: 'Removendo...' })
                          : t('actions.remove', { defaultValue: 'Remover' })}
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving || !pessoaSelecionada}>
            {saving
              ? t('actions.saving')
              : t('kids.children.linkGuardianAction', { defaultValue: 'Vincular responsável' })}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function OcorrenciaDialog({
  open,
  onOpenChange,
  form,
  onChange,
  onSave,
  saving,
  criancasPresentes,
}) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{t('kids.occurrence.register')}</DialogTitle>
          <DialogDescription>
            {t('kids.occurrence.dialogDescription')}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="criancaPessoaId">{t('kids.child')}</Label>
            <Select value={form.criancaPessoaId || 'selecionar'} onValueChange={(value) => onChange('criancaPessoaId', value === 'selecionar' ? '' : value)}>
              <SelectTrigger id="criancaPessoaId">
                <SelectValue placeholder={t('kids.occurrence.selectChild')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="selecionar">{t('kids.occurrence.selectChild')}</SelectItem>
                {criancasPresentes.map((crianca) => (
                  <SelectItem key={crianca.criancaPessoaId} value={String(crianca.criancaPessoaId)}>
                    {crianca.nome}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="tipoOcorrencia">{t('kids.occurrence.type')}</Label>
              <Select value={form.tipo} onValueChange={(value) => onChange('tipo', value)}>
                <SelectTrigger id="tipoOcorrencia">
                  <SelectValue placeholder={t('kids.occurrence.selectType')} />
                </SelectTrigger>
                <SelectContent>
                  {OCORRENCIA_TIPOS.map((tipo) => (
                    <SelectItem key={tipo.value} value={tipo.value}>
                      {formatOcorrenciaTipo(tipo.value, t)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="tituloOcorrencia">{t('kids.occurrence.title')}</Label>
              <Input id="tituloOcorrencia" value={form.titulo} onChange={(e) => onChange('titulo', e.target.value)} placeholder={t('kids.occurrence.titlePlaceholder')} maxLength={200} />
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="descricaoOcorrencia">{t('kids.common.description')}</Label>
            <Textarea id="descricaoOcorrencia" value={form.descricao} onChange={(e) => onChange('descricao', e.target.value)} placeholder={t('kids.occurrence.descriptionPlaceholder')} rows={5} maxLength={2000} />
          </div>

          <div className="grid gap-3 rounded-xl border border-border p-4">
            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-sm font-medium text-foreground">{t('kids.occurrence.requiresGuardianContact')}</p>
                <p className="text-sm text-muted-foreground">{t('kids.occurrence.requiresGuardianContactHint')}</p>
              </div>
              <Switch checked={form.requerContatoResponsavel} onCheckedChange={(checked) => onChange('requerContatoResponsavel', checked)} />
            </div>

            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-sm font-medium text-foreground">{t('kids.occurrence.visibleToGuardian')}</p>
                <p className="text-sm text-muted-foreground">{t('kids.occurrence.visibleToGuardianHint')}</p>
              </div>
              <Switch checked={form.visivelAoResponsavel} onCheckedChange={(checked) => onChange('visivelAoResponsavel', checked)} />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving}>
            {saving ? t('actions.saving') : t('kids.occurrence.register')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function SalaDialog({ open, onOpenChange, form, onChange, onSave, saving }) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('kids.structure.newRoom')}</DialogTitle>
          <DialogDescription>
            {t('kids.structure.roomDialogDescription')}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="salaId">{t('kids.common.identifier')}</Label>
            <Input id="salaId" value={form.id} onChange={(e) => onChange('id', e.target.value)} placeholder={t('kids.structure.roomIdentifierPlaceholder')} maxLength={50} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="salaNome">{t('kids.common.name')}</Label>
            <Input id="salaNome" value={form.nome} onChange={(e) => onChange('nome', e.target.value)} placeholder={t('kids.structure.roomNamePlaceholder')} maxLength={120} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="salaCapacidade">{t('kids.structure.maxCapacity')}</Label>
            <Input id="salaCapacidade" type="number" min="0" value={form.capacidadeMaxima} onChange={(e) => onChange('capacidadeMaxima', e.target.value)} placeholder={t('kids.structure.capacityPlaceholder.room')} />
          </div>
          <div className="flex items-center justify-between gap-3 rounded-xl border border-border p-4">
            <div>
              <p className="text-sm font-medium text-foreground">{t('kids.structure.roomActive')}</p>
              <p className="text-sm text-muted-foreground">{t('kids.structure.roomActiveHint')}</p>
            </div>
            <Switch checked={form.ativo} onCheckedChange={(checked) => onChange('ativo', checked)} />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving}>
            {saving ? t('actions.saving') : t('kids.structure.createRoom')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function TurmaDialog({ open, onOpenChange, form, onChange, onSave, saving, salas }) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t('kids.structure.newClass')}</DialogTitle>
          <DialogDescription>
            {t('kids.structure.classDialogDescription')}
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-2">
          <div className="grid gap-2">
            <Label htmlFor="turmaId">{t('kids.common.identifier')}</Label>
            <Input id="turmaId" value={form.id} onChange={(e) => onChange('id', e.target.value)} placeholder={t('kids.structure.classIdentifierPlaceholder')} maxLength={50} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="turmaSala">{t('kids.common.room')}</Label>
            <Select value={form.salaId || 'selecionar'} onValueChange={(value) => onChange('salaId', value === 'selecionar' ? '' : value)}>
              <SelectTrigger id="turmaSala">
                <SelectValue placeholder={t('kids.common.selectRoom')} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="selecionar">{t('kids.common.selectRoom')}</SelectItem>
                {salas.map((sala) => (
                  <SelectItem key={sala.id} value={sala.id}>
                    {sala.nome}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="turmaNome">{t('kids.common.name')}</Label>
            <Input id="turmaNome" value={form.nome} onChange={(e) => onChange('nome', e.target.value)} placeholder={t('kids.structure.classNamePlaceholder')} maxLength={120} />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="turmaCapacidade">{t('kids.structure.maxCapacity')}</Label>
            <Input id="turmaCapacidade" type="number" min="0" value={form.capacidadeMaxima} onChange={(e) => onChange('capacidadeMaxima', e.target.value)} placeholder={t('kids.structure.capacityPlaceholder.class')} />
          </div>
          <div className="flex items-center justify-between gap-3 rounded-xl border border-border p-4">
            <div>
              <p className="text-sm font-medium text-foreground">{t('kids.structure.classActive')}</p>
              <p className="text-sm text-muted-foreground">{t('kids.structure.classActiveHint')}</p>
            </div>
            <Switch checked={form.ativo} onCheckedChange={(checked) => onChange('ativo', checked)} />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={saving}>
            {t('actions.cancel')}
          </Button>
          <Button onClick={onSave} disabled={saving}>
            {saving ? t('actions.saving') : t('kids.structure.createClass')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function HistoricoDialog({
  open,
  onOpenChange,
  criancaHistorico,
  historicoLoading,
  ocorrenciasHistorico,
  historicoUpdatingId,
  onAtualizarOcorrencia,
  formatDate,
}) {
  const { t } = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>
            {t('kids.history.occurrenceHistory')}
            {criancaHistorico?.nome ? ` • ${criancaHistorico.nome}` : ''}
          </DialogTitle>
          <DialogDescription>
            {t('kids.history.dialogDescription')}
          </DialogDescription>
        </DialogHeader>

        {historicoLoading ? (
          <Loading text={t('kids.history.loadingOccurrences')} />
        ) : ocorrenciasHistorico.length ? (
          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            {ocorrenciasHistorico.map((ocorrencia) => {
              const statusConfig = getOcorrenciaStatusConfig(ocorrencia.status, t);
              const podeMarcarContato = ocorrencia.requerContatoResponsavel && !ocorrencia.contatoResponsavelRealizadoEm;
              const podeEncerrar = !isOcorrenciaEncerrada(ocorrencia.status);

              return (
                <div key={ocorrencia.id} className="rounded-xl border border-border bg-background p-4">
                  <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                    <div className="space-y-2">
                      <div className="flex flex-wrap items-center gap-2">
                        <Badge className={statusConfig.className}>{statusConfig.label}</Badge>
                        <Badge variant="outline">{formatOcorrenciaTipo(ocorrencia.tipo, t)}</Badge>
                        {ocorrencia.visivelAoResponsavel ? <Badge variant="outline">{t('kids.occurrence.visibleToGuardian')}</Badge> : null}
                      </div>
                      <h3 className="font-semibold text-foreground">{ocorrencia.titulo}</h3>
                      <p className="text-sm text-muted-foreground">{ocorrencia.descricao}</p>
                      <div className="flex flex-wrap gap-3 text-xs text-muted-foreground">
                        <span>{t('kids.history.recordedBy', { name: ocorrencia.registradoPorNome })}</span>
                        <span>{formatDate(ocorrencia.dataCriacao)}</span>
                        {ocorrencia.salaId ? <span>{t('kids.history.roomLabel', { room: ocorrencia.salaId })}</span> : null}
                      </div>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      {podeMarcarContato ? (
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={historicoUpdatingId === ocorrencia.id}
                          onClick={() => onAtualizarOcorrencia(ocorrencia.id, { contatoResponsavelRealizado: true })}
                        >
                          <PhoneCall className="mr-2 h-4 w-4" />
                          {t('kids.history.markContact')}
                        </Button>
                      ) : null}
                      {podeEncerrar ? (
                        <Button
                          size="sm"
                          disabled={historicoUpdatingId === ocorrencia.id}
                          onClick={() => onAtualizarOcorrencia(ocorrencia.id, { status: 'Encerrada' })}
                        >
                          <CheckCircle2 className="mr-2 h-4 w-4" />
                          {t('kids.history.close')}
                        </Button>
                      ) : null}
                    </div>
                  </div>

                  <div className="mt-4 grid gap-2 rounded-lg bg-muted/30 p-3 text-sm">
                    <div className="flex items-center gap-2 text-muted-foreground">
                      <PhoneCall className="h-4 w-4" />
                      {ocorrencia.requerContatoResponsavel ? (
                        ocorrencia.contatoResponsavelRealizadoEm ? (
                          <span>
                            {t('kids.history.contactDoneOn', { date: formatDate(ocorrencia.contatoResponsavelRealizadoEm) })}
                            {ocorrencia.contatoResponsavelPorNome ? ` ${t('kids.history.by', { name: ocorrencia.contatoResponsavelPorNome })}` : ''}
                          </span>
                        ) : (
                          <span>{t('kids.history.contactPending')}</span>
                        )
                      ) : (
                        <span>{t('kids.history.contactNotRequired')}</span>
                      )}
                    </div>

                    {ocorrencia.encerradoEm ? (
                      <div className="flex items-center gap-2 text-muted-foreground">
                        <CheckCircle2 className="h-4 w-4" />
                        <span>
                          {t('kids.history.closedOn', { date: formatDate(ocorrencia.encerradoEm) })}
                          {ocorrencia.encerradoPorNome ? ` ${t('kids.history.by', { name: ocorrencia.encerradoPorNome })}` : ''}
                        </span>
                      </div>
                    ) : null}
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <EstadoVazio texto={t('kids.history.noOccurrencesForChild')} />
        )}
      </DialogContent>
    </Dialog>
  );
}
