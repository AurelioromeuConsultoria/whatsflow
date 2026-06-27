export function getApiErrorMessage(error, fallback = 'Erro inesperado') {
  if (!error) return fallback;

  // Axios: error.response?.data pode ser string, objeto, etc.
  const data = error.response?.data;

  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message && String(data.message).trim()) return String(data.message);
  if (data?.error && String(data.error).trim()) return String(data.error);

  // Algumas APIs retornam { errors: [...] } ou { errors: {campo:[...] } }
  const errors = data?.errors;
  if (Array.isArray(errors) && errors.length > 0) return String(errors[0]);
  if (errors && typeof errors === 'object') {
    const firstKey = Object.keys(errors)[0];
    const firstVal = firstKey ? errors[firstKey] : null;
    if (Array.isArray(firstVal) && firstVal.length > 0) return String(firstVal[0]);
    if (typeof firstVal === 'string' && firstVal.trim()) return firstVal;
  }

  if (error.message && String(error.message).trim()) return String(error.message);
  return fallback;
}

