# ADR 0001: Modular monolith

Status: Accepted

ContactCore uses separate Domain, Application, Infrastructure, and Desktop projects in one solution. A microservice architecture would add deployment, privacy, and reliability cost without benefiting a local contact book. The boundaries keep business logic testable and allow persistence/UI replacement later.
