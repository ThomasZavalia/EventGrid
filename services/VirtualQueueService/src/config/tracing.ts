import { NodeSDK } from '@opentelemetry/sdk-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';



const traceExporter = new OTLPTraceExporter({
  url: process.env.JAEGER_ENDPOINT || 'http://jaeger:4317',
});

const sdk = new NodeSDK({
  
  serviceName: 'VirtualQueueService', 
  
  traceExporter,
  instrumentations: [
    getNodeAutoInstrumentations({
      '@opentelemetry/instrumentation-fs': {
        enabled: false,
      },
      '@opentelemetry/instrumentation-http': {
        enabled: true,
      },
      '@opentelemetry/instrumentation-express': {
        enabled: true,
      },
    }),
  ],
});



export const startTracing = () => {
    try {
        sdk.start();
        console.log('🕵️ Tracing inicializado con Jaeger');
    } catch (error) {
        console.error('❌ Error iniciando tracing:', error);
    }
};

process.on('SIGTERM', () => {
  sdk.shutdown()
    .then(() => console.log('✅ Tracing cerrado correctamente'))
    .catch((error) => console.error('❌ Error cerrando tracing:', error));
});