# Modo offline

## Fluxos

- **Combate:** `Offline` -> `Game` -> `Credits`
- **Experiência completa:** `Offline` -> `GloboV2` -> `MedicalQuestions` -> `Credits`

`OfflineSession` guarda a escolha entre cenas e identifica quando os componentes compartilhados devem executar localmente.
`OfflineModeMenu` contém somente os métodos ligados aos botões da cena `Offline`.

O online continua usando Photon. No offline, player, pools, RPCs, projéteis, dano e mudanças de cena usam as alternativas locais implementadas nos mesmos componentes de gameplay.
