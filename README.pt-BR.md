# SprintBoard | Resumo para recrutadores

[🇺🇸 Documentação técnica completa](README.md) · [Perfil GitHub](https://github.com/diego0425)

**Projeto pessoal de portfólio**, não experiência profissional comercial. Aplicação colaborativa Kanban com boards, cards, checklists, convites por e-mail e papéis Owner/Admin/Member.

## Tecnologias e evidências

- **Backend:** C#, .NET 10, ASP.NET Core Web API, Entity Framework Core, SQL Server e Clean Architecture.
- **Frontend:** React 19, TypeScript e Vite.
- **Segurança:** JWT, autorização por participação e papel, hashing de senhas e rate limiting.
- **Testes:** **384 testes automatizados passando**, unitários e de integração. Não significa 100% de cobertura.
- **CI/CD:** GitHub Actions com build, testes, lint, validação Docker e publicação de imagens no GHCR.
- **Infraestrutura:** Docker Compose, Nginx, health checks, logs estruturados e correlation IDs.

## Executar localmente

Requer Docker Desktop e recursos suficientes para SQL Server.

```bash
cp .env.example .env
# Configure os segredos locais seguindo .env.example
docker compose up -d --build
```

Abra http://localhost:3000 após iniciar os contêineres. **Não envie o arquivo .env para o repositório.**

## Status e próximos passos

- Ambiente Docker local documentado.
- Azure Container Apps, Azure SQL e Blob Storage são a **arquitetura de deploy planejada**, ainda não publicada.
- Capturas de tela e demonstração pública serão adicionadas quando disponíveis.
- Detalhes de arquitetura, testes, autenticação, segurança, endpoints e CI/CD estão no [README completo](README.md).
