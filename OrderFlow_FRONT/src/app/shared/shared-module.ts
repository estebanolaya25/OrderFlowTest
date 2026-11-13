import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Avisos } from './componentes/avisos/avisos';
import { BarraCarga } from './componentes/barra-carga/barra-carga';
import { EstadoPedidoInsignia } from './componentes/estado-pedido/estado-pedido';
import { Navbar } from './componentes/navbar/navbar';
import { PiePagina } from './componentes/pie-pagina/pie-pagina';
import { NoEncontrada } from './paginas/no-encontrada/no-encontrada';

const COMPARTIDOS = [Navbar, PiePagina, NoEncontrada, Avisos, BarraCarga, EstadoPedidoInsignia];

// Componentes que usan varios módulos. Se importa en AppModule y en cada módulo de funcionalidad.
@NgModule({
  declarations: COMPARTIDOS,
  imports: [CommonModule, RouterModule],
  exports: [CommonModule, RouterModule, ...COMPARTIDOS]
})
export class SharedModule {}
