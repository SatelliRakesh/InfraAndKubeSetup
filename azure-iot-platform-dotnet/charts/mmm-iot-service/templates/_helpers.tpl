{{/* vim: set filetype=mustache: */}}
{{- define "service.name" -}}
  {{- required "Error: missing required value .Values.nameOverride" .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "service.fullname" -}}
  {{- include "service.name" . -}}
{{- end -}}

{{- define "service.chart" -}}
  {{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "service.labels" -}}
app.kubernetes.io/name: {{ include "service.name" . }}
helm.sh/chart: {{ include "service.chart" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{- define "service.deployment.image.repository" -}}
  {{- include "service.name" . | cat "azureiot3m/" | nospace -}}
{{- end -}}
