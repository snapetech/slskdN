// <copyright file="swarmAnalytics.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import api from './api';

const asArray = (value) => (Array.isArray(value) ? value : []);

/**
 * Get the complete swarm analytics dashboard from one server snapshot.
 * @param {number} timeWindowHours - Time window in hours.
 * @param {number} rankingLimit - Maximum peer rankings to return.
 * @returns {Promise<object>} Complete analytics dashboard
 */
export const getDashboard = async (
  timeWindowHours = 24,
  rankingLimit = 20,
) => {
  const response = await api.get('/swarm/analytics/dashboard', {
    params: { rankingLimit, timeWindowHours },
  });
  return response.data;
};

/**
 * Get swarm performance metrics.
 * @param {number} timeWindowHours - Time window in hours (default: 24)
 * @returns {Promise<object>} Performance metrics
 */
export const getPerformanceMetrics = async (timeWindowHours = 24) => {
  try {
    const response = await api.get(
      `/swarm/analytics/performance?timeWindowHours=${timeWindowHours}`,
    );
    return response.data;
  } catch (error) {
    console.error('Failed to fetch performance metrics:', error);
    throw error;
  }
};

/**
 * Get peer performance rankings.
 * @param {number} limit - Maximum number of peers to return (default: 20)
 * @returns {Promise<Array>} Peer rankings
 */
export const getPeerRankings = async (limit = 20) => {
  try {
    const response = await api.get(
      `/swarm/analytics/peers/rankings?limit=${limit}`,
    );
    return asArray(response.data);
  } catch (error) {
    console.error('Failed to fetch peer rankings:', error);
    throw error;
  }
};

/**
 * Get swarm efficiency metrics.
 * @param {number} timeWindowHours - Time window in hours (default: 24)
 * @returns {Promise<object>} Efficiency metrics
 */
export const getEfficiencyMetrics = async (timeWindowHours = 24) => {
  try {
    const response = await api.get(
      `/swarm/analytics/efficiency?timeWindowHours=${timeWindowHours}`,
    );
    return response.data;
  } catch (error) {
    console.error('Failed to fetch efficiency metrics:', error);
    throw error;
  }
};

/**
 * Get historical trends for swarm metrics.
 * @param {number} timeWindowHours - Time window in hours (default: 24)
 * @param {number} dataPoints - Number of data points (default: 24)
 * @returns {Promise<object>} Trend data
 */
export const getTrends = async (timeWindowHours = 24, dataPoints = 24) => {
  try {
    const response = await api.get(
      `/swarm/analytics/trends?timeWindowHours=${timeWindowHours}&dataPoints=${dataPoints}`,
    );
    return response.data;
  } catch (error) {
    console.error('Failed to fetch trends:', error);
    throw error;
  }
};

/**
 * Get recommendations for optimizing swarm performance.
 * @returns {Promise<Array>} Recommendations
 */
export const getRecommendations = async () => {
  try {
    const response = await api.get('/swarm/analytics/recommendations');
    return asArray(response.data);
  } catch (error) {
    console.error('Failed to fetch recommendations:', error);
    throw error;
  }
};
