import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PedidoNuevo } from './paginas/pedido-nuevo/pedido-nuevo';
import { PedidosLista } from './paginas/pedidos-lista/pedidos-lista';

const routes: Routes = [
  { path: '', component: PedidosLista, title: 'Pedidos · OrderFlow' },
  { path: 'nuevo', component: PedidoNuevo, title: 'Nuevo pedido · OrderFlow' }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PedidosRoutingModule {}
