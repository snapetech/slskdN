import React from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter as Router } from "react-router-dom";
import 'semantic-ui-css/semantic.min.css';
import App from './components/App';

createRoot(document.getElementById('root')).render(
    <Router>
        <App />
    </Router>
);
