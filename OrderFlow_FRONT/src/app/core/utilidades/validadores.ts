import { AbstractControl, ValidationErrors } from '@angular/forms';

// "   " pasa Validators.required pero el backend lo rechaza: se valida igual aquí.
export function sinSoloEspacios(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  return typeof valor === 'string' && valor.length > 0 && valor.trim().length === 0 ? { soloEspacios: true } : null;
}

// El input type="number" acepta decimales (2.5); la cantidad debe ser entera.
export function entero(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  return valor === null || valor === '' || Number.isInteger(valor) ? null : { entero: true };
}
