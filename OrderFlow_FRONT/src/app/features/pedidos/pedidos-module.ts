import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../../shared/shared-module';
import { PedidoNuevo } from './paginas/pedido-nuevo/pedido-nuevo';
import { PedidosLista } from './paginas/pedidos-lista/pedidos-lista';
import { PedidosRoutingModule } from './pedidos-routing-module';

@NgModule({
  declarations: [PedidosLista, PedidoNuevo],
  imports: [SharedModule, ReactiveFormsModule, PedidosRoutingModule]
})
export class PedidosModule {}
