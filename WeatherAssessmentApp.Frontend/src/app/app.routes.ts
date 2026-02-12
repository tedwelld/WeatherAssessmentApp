import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ForecastComponent } from './pages/forecast/forecast.component';

export const routes: Routes = [
  {
    path: '',
    component: DashboardComponent
  },
  {
    path: 'forecast/:id',
    component: ForecastComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];
