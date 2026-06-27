import { BarChart3, MapPinned, PieChart } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { ChartContainer, ChartTooltip, ChartTooltipContent } from '@/components/ui/chart';
import { Bar, BarChart, CartesianGrid, Cell, Pie, PieChart as RechartsPieChart, XAxis, YAxis } from 'recharts';

const chartColors = [
  'var(--color-chart-1)',
  'var(--color-chart-2)',
  'var(--color-chart-3)',
  'var(--color-chart-4)',
  'var(--color-chart-5)',
];

export default function PatrimonioRelatorioCharts({
  t,
  consolidadoPorCategoria,
  consolidadoPorStatus,
  consolidadoPorLocal,
  statusKeyMap,
  formatCurrency,
}) {
  const graficoCategorias = consolidadoPorCategoria
    .slice(0, 6)
    .map((item, index) => ({
      nome: item.nome,
      valorTotal: Number(item.valorTotal || 0),
      fill: chartColors[index % chartColors.length],
    }));

  const graficoStatus = consolidadoPorStatus
    .map((item, index) => ({
      nome: t(`finance.patrimony.status.${statusKeyMap[item.status] || 'inUse'}`),
      quantidade: Number(item.quantidade || 0),
      fill: chartColors[index % chartColors.length],
    }));

  const graficoLocais = consolidadoPorLocal
    .slice(0, 6)
    .map((item, index) => ({
      nome: item.localizacao,
      quantidade: Number(item.quantidade || 0),
      fill: chartColors[index % chartColors.length],
    }));

  const categoryChartConfig = graficoCategorias.reduce((acc, item) => {
    acc[item.nome] = { label: item.nome, color: item.fill };
    return acc;
  }, { valorTotal: { label: t('finance.patrimonyReport.table.value'), color: chartColors[0] } });

  const statusChartConfig = graficoStatus.reduce((acc, item) => {
    acc[item.nome] = { label: item.nome, color: item.fill };
    return acc;
  }, { quantidade: { label: t('finance.patrimonyReport.table.quantity'), color: chartColors[1] } });

  const locationChartConfig = graficoLocais.reduce((acc, item) => {
    acc[item.nome] = { label: item.nome, color: item.fill };
    return acc;
  }, { quantidade: { label: t('finance.patrimonyReport.table.quantity'), color: chartColors[2] } });

  return (
    <div className="grid gap-6 xl:grid-cols-3">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="h-4 w-4" />
            {t('finance.patrimonyReport.charts.byCategory')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {graficoCategorias.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
          ) : (
            <ChartContainer config={categoryChartConfig} className="h-[260px] w-full">
              <BarChart accessibilityLayer data={graficoCategorias} margin={{ left: 12, right: 12, top: 8 }}>
                <CartesianGrid vertical={false} />
                <XAxis dataKey="nome" tickLine={false} axisLine={false} interval={0} angle={-20} textAnchor="end" height={60} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={(value) => `R$ ${Math.round(value / 1000)}k`} />
                <ChartTooltip content={<ChartTooltipContent formatter={(value) => formatCurrency(value)} />} />
                <Bar dataKey="valorTotal" radius={8}>
                  {graficoCategorias.map((entry) => (
                    <Cell key={entry.nome} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ChartContainer>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PieChart className="h-4 w-4" />
            {t('finance.patrimonyReport.charts.byStatus')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {graficoStatus.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
          ) : (
            <ChartContainer config={statusChartConfig} className="mx-auto h-[260px] w-full">
              <RechartsPieChart>
                <ChartTooltip content={<ChartTooltipContent formatter={(value) => `${value} ${t('finance.patrimonyReport.units.items')}`} />} />
                <Pie data={graficoStatus} dataKey="quantidade" nameKey="nome" innerRadius={55} outerRadius={85}>
                  {graficoStatus.map((entry) => (
                    <Cell key={entry.nome} fill={entry.fill} />
                  ))}
                </Pie>
              </RechartsPieChart>
            </ChartContainer>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MapPinned className="h-4 w-4" />
            {t('finance.patrimonyReport.charts.byLocation')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {graficoLocais.length === 0 ? (
            <div className="py-8 text-center text-muted-foreground">{t('finance.patrimonyReport.empty')}</div>
          ) : (
            <ChartContainer config={locationChartConfig} className="h-[260px] w-full">
              <BarChart accessibilityLayer data={graficoLocais} layout="vertical" margin={{ left: 8, right: 16 }}>
                <CartesianGrid horizontal={false} />
                <XAxis type="number" hide />
                <YAxis dataKey="nome" type="category" tickLine={false} axisLine={false} width={96} />
                <ChartTooltip content={<ChartTooltipContent formatter={(value) => `${value} ${t('finance.patrimonyReport.units.items')}`} />} />
                <Bar dataKey="quantidade" radius={8}>
                  {graficoLocais.map((entry) => (
                    <Cell key={entry.nome} fill={entry.fill} />
                  ))}
                </Bar>
              </BarChart>
            </ChartContainer>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
