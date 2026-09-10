/** Görüntüleme biçimleri. İş mantığı değil — backend ne gönderdiyse onu okunur hale getirir. */

const money = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' })
const percent = new Intl.NumberFormat('tr-TR', { style: 'percent', maximumFractionDigits: 2 })
const dateTime = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' })

export const formatMoney = (value: number) => money.format(value)

export const formatPercent = (rate: number) => percent.format(rate)

export const formatDateTime = (value: string | null | undefined) =>
  value ? dateTime.format(new Date(value)) : '—'
