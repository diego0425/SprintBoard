import axios from "axios";

const AUTH_STORAGE_KEY = "accessToken";

export const api = axios.create({
  baseURL: "http://localhost:5277/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(AUTH_STORAGE_KEY);

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(AUTH_STORAGE_KEY);

      if (window.location.pathname !== "/login") {
        window.location.replace("/login");
      }
    }

    return Promise.reject(error);
  }
);
