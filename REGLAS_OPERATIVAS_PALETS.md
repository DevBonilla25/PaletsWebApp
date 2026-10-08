# Reglas operativas de pallets

## Identificación

- El pallet se identifica por una descripción pintada y única, por ejemplo `Palet #150` o `Palet Bloque #150`.
- En esta fase no se utilizarán códigos QR porque la operación puede deteriorarlos o perderlos.

## Operaciones

| Operación | Origen | Destino | Confirmación |
|---|---|---|---|
| Entrega interna | Bodega | Chofer | Obligatoria por el chofer |
| Devolución interna | Chofer | Bodega | Obligatoria por el bodeguero |
| Entrega comercial | Chofer | Cliente | Por ahora utiliza la transferencia normal; se integrará otro sistema en el futuro |
| Recuperación | Cliente o tercero | Chofer | Reclamo validado posteriormente por un administrador |
| Retiro fuera de horario | Bodega | Chofer externo | Reclamo provisional validado posteriormente por un administrador |

## Estados y restricciones actuales

- Una transferencia normal puede incluir pallets `Disponible` o `Reclamado`.
- Al aceptar una transferencia normal, el pallet pasa al receptor como `Disponible`.
- Un reclamo puede incluir pallets `Disponible` o `Reclamado` que pertenezcan a otro usuario.
- Al aprobar un pallet `Reclamado`, la custodia pasa al reclamante; al rechazar o anular, conserva su estado y custodio anterior.
- Distintos usuarios pueden reclamar simultáneamente el mismo pallet; el primer reclamo aprobado obtiene la custodia y los detalles competidores se cierran como no adjudicados.
- Un mismo usuario no puede mantener dos reclamos pendientes sobre el mismo pallet.
- Los estados `En transferencia` y `En reclamo` bloquean nuevas operaciones.
- Solo un administrador puede aceptar o rechazar un reclamo.
- El creador puede anular su reclamo mientras permanezca `Por reclamar`.

## Auditoría futura

- Se conserva como referencia que una integración futura podrá distinguir el flujo de clientes del flujo obligatorio entre empleados.
- Todas las transferencias, incluidas las realizadas a clientes, se aceptan automáticamente después de 48 horas si el receptor no las acepta ni rechaza.
- La aceptación automática procesa únicamente detalles todavía pendientes y registra `Sistema` como responsable.
- Las transferencias vencidas se procesan una vez al día, en el cierre de medianoche de Ecuador.

- Se identificarán pallets sin movimientos durante una cantidad de días configurada por un administrador.
- La auditoría debe distinguir pallets registrados, verificados, ubicados y no localizados.
- Esta capacidad requiere historial de movimientos y configuración; no forma parte de la fase sin migraciones.
