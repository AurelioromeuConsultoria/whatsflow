import { useState, useCallback } from 'react';

/**
 * Hook para validação de formulários em tempo real
 * 
 * @param {Object} validationRules - Regras de validação: { fieldName: { required, minLength, maxLength, pattern, custom } }
 * @param {Object} initialValues - Valores iniciais do formulário
 * @returns {Object} - { values, errors, touched, handleChange, handleBlur, validate, isValid, reset }
 */
export function useFormValidation(validationRules = {}, initialValues = {}) {
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});

  const validateField = useCallback((fieldName, value) => {
    const rules = validationRules[fieldName];
    if (!rules) return null;

    const fieldErrors = [];

    // Required
    if (rules.required && (!value || (typeof value === 'string' && value.trim() === ''))) {
      fieldErrors.push(rules.requiredMessage || `${fieldName} é obrigatório`);
    }

    // MinLength
    if (rules.minLength && value && value.length < rules.minLength) {
      fieldErrors.push(rules.minLengthMessage || `Mínimo de ${rules.minLength} caracteres`);
    }

    // MaxLength
    if (rules.maxLength && value && value.length > rules.maxLength) {
      fieldErrors.push(rules.maxLengthMessage || `Máximo de ${rules.maxLength} caracteres`);
    }

    // Pattern (regex)
    if (rules.pattern && value && !rules.pattern.test(value)) {
      fieldErrors.push(rules.patternMessage || `Formato inválido`);
    }

    // Email
    if (rules.email && value) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(value)) {
        fieldErrors.push(rules.emailMessage || 'Email inválido');
      }
    }

    // URL
    if (rules.url && value) {
      try {
        // Se não começa com http/https, adicionar
        const urlToTest = value.startsWith('http') ? value : `https://${value}`;
        new URL(urlToTest);
      } catch {
        // Se começa com /, é uma URL relativa válida
        if (!value.startsWith('/')) {
          fieldErrors.push(rules.urlMessage || 'URL inválida');
        }
      }
    }

    // Custom validation function
    if (rules.custom && value) {
      const customError = rules.custom(value, values);
      if (customError) {
        fieldErrors.push(customError);
      }
    }

    return fieldErrors.length > 0 ? fieldErrors[0] : null;
  }, [validationRules, values]);

  const validate = useCallback((fieldsToValidate = null) => {
    const fields = fieldsToValidate || Object.keys(validationRules);
    const newErrors = {};

    fields.forEach(fieldName => {
      const error = validateField(fieldName, values[fieldName]);
      if (error) {
        newErrors[fieldName] = error;
      }
    });

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [values, validateField, validationRules]);

  const handleChange = useCallback((fieldName, value) => {
    setValues(prev => ({ ...prev, [fieldName]: value }));

    // Validar em tempo real se o campo já foi tocado
    if (touched[fieldName]) {
      const error = validateField(fieldName, value);
      setErrors(prev => ({
        ...prev,
        [fieldName]: error || undefined,
      }));
    }
  }, [touched, validateField]);

  const handleBlur = useCallback((fieldName) => {
    setTouched(prev => ({ ...prev, [fieldName]: true }));

    // Validar quando o campo perde o foco
    const error = validateField(fieldName, values[fieldName]);
    setErrors(prev => ({
      ...prev,
      [fieldName]: error || undefined,
    }));
  }, [values, validateField]);

  const reset = useCallback((newValues = initialValues) => {
    setValues(newValues);
    setErrors({});
    setTouched({});
  }, [initialValues]);

  const isValid = Object.keys(errors).length === 0 && Object.keys(touched).length > 0;

  return {
    values,
    errors,
    touched,
    handleChange,
    handleBlur,
    validate,
    isValid,
    reset,
    setValues,
  };
}
