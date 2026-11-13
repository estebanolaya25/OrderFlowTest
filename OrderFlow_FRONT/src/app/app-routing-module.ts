import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NoEncontrada } from './shared/paginas/no-encontrada/no-encontrada';

// El módulo de pedidos se descarga cuando se entra a su ruta (lazy loading).
const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  { path: 'pedidos', loadChildren: () => import('./features/pedidos/pedidos-module').then(m => m.PedidosModule) },
  { path: '**', component: NoEncontrada, title: 'Página no encontrada · OrderFlow' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top' })],
  exports: [RouterModule]
})
export class AppRoutingModule {}
