import { useEffect, useMemo, useRef, useState } from 'react';
import { Search, Filter, X, ChevronDown, ChevronUp } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Label } from '@/components/ui/label';
import { useDebouncedCallback } from '@/hooks/useDebouncedCallback';
import { useTranslation } from 'react-i18next';

/**
 * Componente de busca avançada reutilizável
 * 
 * @param {Object} props
 * @param {Array} props.searchFields - Campos disponíveis para busca: [{ key: 'nome', label: 'Nome', type: 'text' }, ...]
 * @param {Array} props.filterFields - Campos disponíveis para filtros: [{ key: 'status', label: 'Status', type: 'select', options: [...] }, ...]
 * @param {Object} props.values - Valores atuais dos filtros
 * @param {Function} props.onChange - Callback quando valores mudam: (values) => void
 * @param {Function} props.onReset - Callback para resetar filtros: () => void
 * @param {boolean} props.defaultOpen - Se os filtros avançados devem estar abertos por padrão
 */
export function AdvancedSearch({
  searchFields = [],
  filterFields = [],
  values = {},
  onChange = () => {},
  onReset = () => {},
  defaultOpen = false,
}) {
  const { t } = useTranslation();
  const [isOpen, setIsOpen] = useState(defaultOpen);
  const valuesRef = useRef(values);

  useEffect(() => {
    valuesRef.current = values;
  }, [values]);

  const searchKeys = useMemo(() => searchFields.map((f) => f.key), [searchFields]);
  const searchKeysSig = useMemo(() => searchKeys.join('|'), [searchKeys]);
  const [localSearch, setLocalSearch] = useState(() => {
    const initial = {};
    searchKeys.forEach((k) => {
      initial[k] = values[k] || '';
    });
    return initial;
  });

  useEffect(() => {
    setLocalSearch((prev) => {
      const next = { ...prev };
      searchKeys.forEach((k) => {
        next[k] = values[k] || '';
      });
      return next;
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchKeysSig, values]);

  const debouncedApplySearchChange = useDebouncedCallback((fieldKey, value) => {
    onChange({
      ...valuesRef.current,
      [fieldKey]: value,
    });
  }, 300);

  const handleSearchChange = (fieldKey, value) => {
    onChange({
      ...valuesRef.current,
      [fieldKey]: value,
    });
  };

  const handleFilterChange = (fieldKey, value) => {
    onChange({
      ...values,
      [fieldKey]: value === 'all' || value === '' ? undefined : value,
    });
  };

  const handleReset = () => {
    const resetValues = {};
    searchFields.forEach(field => {
      resetValues[field.key] = '';
    });
    filterFields.forEach(field => {
      resetValues[field.key] = undefined;
    });
    onChange(resetValues);
    onReset();
  };

  const hasActiveFilters = () => {
    return Object.values(values).some(value => 
      value !== undefined && value !== null && value !== '' && value !== 'all'
    );
  };

  const activeFiltersCount = Object.values(values).filter(value => 
    value !== undefined && value !== null && value !== '' && value !== 'all'
  ).length;

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <CardTitle className="flex min-w-0 items-center gap-2 text-base">
            <Search className="h-4 w-4" />
            <span className="truncate">{t('advancedSearch.title')}</span>
            {activeFiltersCount > 0 && (
              <span className="shrink-0 rounded-full bg-primary px-2 py-0.5 text-xs text-primary-foreground">
                {activeFiltersCount}
              </span>
            )}
          </CardTitle>
          <div className="flex flex-wrap items-center gap-2">
            {hasActiveFilters() && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleReset}
                className="h-8 text-xs"
              >
                <X className="h-3 w-3 mr-1" />
                {t('advancedSearch.clear')}
              </Button>
            )}
            {filterFields.length > 0 && (
              <Collapsible open={isOpen} onOpenChange={setIsOpen}>
                <CollapsibleTrigger asChild>
                  <Button variant="ghost" size="sm" className="h-8">
                    {isOpen ? (
                      <>
                        <ChevronUp className="h-4 w-4 mr-1" />
                        {t('advancedSearch.hide')}
                      </>
                    ) : (
                      <>
                        <ChevronDown className="h-4 w-4 mr-1" />
                        {t('advancedSearch.filters')}
                      </>
                    )}
                  </Button>
                </CollapsibleTrigger>
              </Collapsible>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Campos de busca */}
        {searchFields.length > 0 && (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {searchFields.map((field) => (
              <div key={field.key} className="space-y-2">
                <Label className="text-sm font-medium flex items-center gap-2">
                  <Search className="h-3 w-3" />
                  {field.label}
                </Label>
                {field.type === 'text' || !field.type ? (
                  <Input
                    value={localSearch[field.key] ?? ''}
                    onChange={(e) => {
                      const v = e.target.value;
                      setLocalSearch((prev) => ({ ...prev, [field.key]: v }));
                      debouncedApplySearchChange(field.key, v);
                    }}
                    placeholder={field.placeholder || t('advancedSearch.searchPlaceholder', { field: String(field.label).toLowerCase() })}
                    className="w-full"
                  />
                ) : field.type === 'date' ? (
                  <Input
                    type="date"
                    value={values[field.key] || ''}
                    onChange={(e) => handleSearchChange(field.key, e.target.value)}
                    className="w-full"
                  />
                ) : field.type === 'select' ? (
                  <Select
                    value={values[field.key] || 'all'}
                    onValueChange={(value) => handleSearchChange(field.key, value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder={field.placeholder || t('advancedSearch.selectPlaceholder', { field: String(field.label).toLowerCase() })} />
                    </SelectTrigger>
                    <SelectContent>
                      {field.options && field.options.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : null}
              </div>
            ))}
          </div>
        )}

        {/* Filtros avançados (colapsáveis) */}
        {filterFields.length > 0 && (
          <Collapsible open={isOpen} onOpenChange={setIsOpen}>
            <CollapsibleContent>
              <div className="pt-4 border-t space-y-4">
                <div className="flex items-center gap-2 mb-3">
                  <Filter className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium text-muted-foreground">{t('advancedSearch.advancedFilters')}</span>
                </div>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {filterFields.map((field) => (
                    <div key={field.key} className="space-y-2">
                      <Label className="text-sm font-medium">{field.label}</Label>
                      {field.type === 'select' ? (
                        <Select
                          value={values[field.key] || 'all'}
                          onValueChange={(value) => handleFilterChange(field.key, value)}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder={field.placeholder || t('advancedSearch.all')} />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="all">{t('advancedSearch.all')}</SelectItem>
                            {field.options && field.options.map((option) => (
                              <SelectItem key={option.value} value={String(option.value)}>
                                {option.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      ) : field.type === 'date-range' ? (
                        <div className="grid gap-2 sm:grid-cols-2">
                          <div className="space-y-1">
                            <Label className="text-xs text-muted-foreground">{t('advancedSearch.from')}</Label>
                            <Input
                              type="date"
                              value={values[`${field.key}_from`] || ''}
                              onChange={(e) => handleSearchChange(`${field.key}_from`, e.target.value)}
                              className="w-full"
                            />
                          </div>
                          <div className="space-y-1">
                            <Label className="text-xs text-muted-foreground">{t('advancedSearch.to')}</Label>
                            <Input
                              type="date"
                              value={values[`${field.key}_to`] || ''}
                              onChange={(e) => handleSearchChange(`${field.key}_to`, e.target.value)}
                              className="w-full"
                            />
                          </div>
                        </div>
                      ) : field.type === 'boolean' ? (
                        <Select
                          value={values[field.key] || 'all'}
                          onValueChange={(value) => handleFilterChange(field.key, value)}
                        >
                          <SelectTrigger>
                            <SelectValue placeholder={t('advancedSearch.all')} />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="all">{t('advancedSearch.all')}</SelectItem>
                            <SelectItem value="true">{field.trueLabel || t('advancedSearch.yes')}</SelectItem>
                            <SelectItem value="false">{field.falseLabel || t('advancedSearch.no')}</SelectItem>
                          </SelectContent>
                        </Select>
                      ) : null}
                    </div>
                  ))}
                </div>
              </div>
            </CollapsibleContent>
          </Collapsible>
        )}
      </CardContent>
    </Card>
  );
}
