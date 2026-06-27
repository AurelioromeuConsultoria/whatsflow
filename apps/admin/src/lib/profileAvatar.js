const AVATAR_EVENT = 'profile-avatar-updated';

export function getProfileAvatarKey(usuario) {
  const identity = usuario?.id || usuario?.usuarioId || usuario?.email || usuario?.emailLogin || 'anon';
  return `profile-avatar:${identity}`;
}

export function getProfileAvatar(usuario) {
  if (!usuario) return null;
  return localStorage.getItem(getProfileAvatarKey(usuario));
}

export function setProfileAvatar(usuario, dataUrl) {
  if (!usuario || !dataUrl) return;
  localStorage.setItem(getProfileAvatarKey(usuario), dataUrl);
  window.dispatchEvent(new CustomEvent(AVATAR_EVENT, { detail: { key: getProfileAvatarKey(usuario), dataUrl } }));
}

export function removeProfileAvatar(usuario) {
  if (!usuario) return;
  localStorage.removeItem(getProfileAvatarKey(usuario));
  window.dispatchEvent(new CustomEvent(AVATAR_EVENT, { detail: { key: getProfileAvatarKey(usuario), dataUrl: null } }));
}

export { AVATAR_EVENT };
