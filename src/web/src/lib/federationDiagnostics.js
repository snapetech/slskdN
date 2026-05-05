// <copyright file="federationDiagnostics.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';

export const getDiagnostics = () => api.get('/federation/diagnostics');
