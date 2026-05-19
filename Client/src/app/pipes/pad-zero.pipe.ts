import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'padZero',
  standalone: true,
})
export class PadZeroPipe implements PipeTransform {
  transform(value: number | string): string {
    const num = typeof value === 'string' ? parseInt(value, 10) : value;
    return num < 10 ? `0${num}` : `${num}`;
  }
}
