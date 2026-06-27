import React from 'react';
import {
  AlertTriangle,
  Calendar,
  Clock,
  FileClock,
  LogIn,
  LogOut,
  PlusCircle,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useTranslation } from 'react-i18next';
import { formatDateTime } from '@/lib/formatters';

export function ResumoCard({ title, value, description, icon, valueClassName }) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        {React.createElement(icon, { className: 'h-4 w-4 text-muted-foreground' })}
      </CardHeader>
      <CardContent>
        <div className={`text-2xl font-bold ${valueClassName || ''}`}>{value}</div>
        <p className="text-xs text-muted-foreground">{description}</p>
      </CardContent>
    </Card>
  );
}

export function IndicadorLinha({ label, value }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-lg bg-muted/30 px-3 py-2">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-semibold text-foreground">{value}</span>
    </div>
  );
}

export function PainelCriancaCard({ crianca, onRegistrarOcorrencia, onVerHistorico }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-xl border border-border bg-background p-4 shadow-sm">
      <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div className="space-y-2">
          <div className="flex items-center gap-2">
            <span className="font-semibold text-foreground">{crianca.nome}</span>
            {crianca.temAlergia || crianca.temRestricao || crianca.temObservacaoCritica ? (
              <Badge className="bg-red-500 hover:bg-red-600">
                <AlertTriangle className="mr-1 h-3 w-3" />
                {t('kids.panel.attention')}
              </Badge>
            ) : null}
            {crianca.retiradaEmModoExcecao ? (
              <Badge className="bg-orange-500 hover:bg-orange-600">{t('kids.panel.exception')}</Badge>
            ) : null}
          </div>
          <div className="flex flex-wrap gap-3 text-sm text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <LogIn className="h-4 w-4" />
              {formatDateTime(crianca.checkinTime, '-', {
                hour: '2-digit',
                minute: '2-digit',
              })}
            </span>
            <span className="inline-flex items-center gap-1">
              <Calendar className="h-4 w-4" />
              {crianca.salaId || t('kids.children.noRoom')}
            </span>
            <span className="inline-flex items-center gap-1">
              <Clock className="h-4 w-4" />
              {crianca.tokenRetiradaAtivo ? t('kids.panel.activeToken') : t('kids.panel.unavailableToken')}
            </span>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          {crianca.temAlergia ? <Badge variant="outline">{t('kids.panel.allergy')}</Badge> : null}
          {crianca.temRestricao ? <Badge variant="outline">{t('kids.panel.restriction')}</Badge> : null}
          {crianca.temObservacaoCritica ? <Badge variant="outline">{t('kids.panel.criticalNote')}</Badge> : null}
        </div>
      </div>

      <div className="mt-4 flex flex-wrap gap-2">
        <Button variant="outline" size="sm" onClick={onRegistrarOcorrencia}>
          <PlusCircle className="mr-2 h-4 w-4" />
          {t('kids.occurrence.register')}
        </Button>
        <Button variant="ghost" size="sm" onClick={onVerHistorico}>
          <FileClock className="mr-2 h-4 w-4" />
          {t('kids.history.view')}
        </Button>
      </div>
    </div>
  );
}

export function EstadoVazio({ texto }) {
  return (
    <div className="rounded-xl border border-dashed border-border bg-muted/20 p-8 text-center text-sm text-muted-foreground">
      {texto}
    </div>
  );
}

export function CheckPanelIcon(props) {
  return <LogOut {...props} />;
}
