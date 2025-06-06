// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: opentelemetry/proto/collector/metrics/v1/metrics_service.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace OpenTelemetry.Proto.Collector.Metrics.V1 {

  /// <summary>Holder for reflection information generated from opentelemetry/proto/collector/metrics/v1/metrics_service.proto</summary>
  public static partial class MetricsServiceReflection {

    #region Descriptor
    /// <summary>File descriptor for opentelemetry/proto/collector/metrics/v1/metrics_service.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static MetricsServiceReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cj5vcGVudGVsZW1ldHJ5L3Byb3RvL2NvbGxlY3Rvci9tZXRyaWNzL3YxL21l",
            "dHJpY3Nfc2VydmljZS5wcm90bxIob3BlbnRlbGVtZXRyeS5wcm90by5jb2xs",
            "ZWN0b3IubWV0cmljcy52MRosb3BlbnRlbGVtZXRyeS9wcm90by9tZXRyaWNz",
            "L3YxL21ldHJpY3MucHJvdG8iaAobRXhwb3J0TWV0cmljc1NlcnZpY2VSZXF1",
            "ZXN0EkkKEHJlc291cmNlX21ldHJpY3MYASADKAsyLy5vcGVudGVsZW1ldHJ5",
            "LnByb3RvLm1ldHJpY3MudjEuUmVzb3VyY2VNZXRyaWNzIn4KHEV4cG9ydE1l",
            "dHJpY3NTZXJ2aWNlUmVzcG9uc2USXgoPcGFydGlhbF9zdWNjZXNzGAEgASgL",
            "MkUub3BlbnRlbGVtZXRyeS5wcm90by5jb2xsZWN0b3IubWV0cmljcy52MS5F",
            "eHBvcnRNZXRyaWNzUGFydGlhbFN1Y2Nlc3MiUgobRXhwb3J0TWV0cmljc1Bh",
            "cnRpYWxTdWNjZXNzEhwKFHJlamVjdGVkX2RhdGFfcG9pbnRzGAEgASgDEhUK",
            "DWVycm9yX21lc3NhZ2UYAiABKAkyrAEKDk1ldHJpY3NTZXJ2aWNlEpkBCgZF",
            "eHBvcnQSRS5vcGVudGVsZW1ldHJ5LnByb3RvLmNvbGxlY3Rvci5tZXRyaWNz",
            "LnYxLkV4cG9ydE1ldHJpY3NTZXJ2aWNlUmVxdWVzdBpGLm9wZW50ZWxlbWV0",
            "cnkucHJvdG8uY29sbGVjdG9yLm1ldHJpY3MudjEuRXhwb3J0TWV0cmljc1Nl",
            "cnZpY2VSZXNwb25zZSIAQqQBCitpby5vcGVudGVsZW1ldHJ5LnByb3RvLmNv",
            "bGxlY3Rvci5tZXRyaWNzLnYxQhNNZXRyaWNzU2VydmljZVByb3RvUAFaM2dv",
            "Lm9wZW50ZWxlbWV0cnkuaW8vcHJvdG8vb3RscC9jb2xsZWN0b3IvbWV0cmlj",
            "cy92MaoCKE9wZW5UZWxlbWV0cnkuUHJvdG8uQ29sbGVjdG9yLk1ldHJpY3Mu",
            "VjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::OpenTelemetry.Proto.Metrics.V1.MetricsReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceRequest), global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceRequest.Parser, new[]{ "ResourceMetrics" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceResponse), global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsServiceResponse.Parser, new[]{ "PartialSuccess" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess), global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess.Parser, new[]{ "RejectedDataPoints", "ErrorMessage" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class ExportMetricsServiceRequest : pb::IMessage<ExportMetricsServiceRequest>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ExportMetricsServiceRequest> _parser = new pb::MessageParser<ExportMetricsServiceRequest>(() => new ExportMetricsServiceRequest());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ExportMetricsServiceRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::OpenTelemetry.Proto.Collector.Metrics.V1.MetricsServiceReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceRequest(ExportMetricsServiceRequest other) : this() {
      resourceMetrics_ = other.resourceMetrics_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceRequest Clone() {
      return new ExportMetricsServiceRequest(this);
    }

    /// <summary>Field number for the "resource_metrics" field.</summary>
    public const int ResourceMetricsFieldNumber = 1;
    private static readonly pb::FieldCodec<global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics> _repeated_resourceMetrics_codec
        = pb::FieldCodec.ForMessage(10, global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics.Parser);
    private readonly pbc::RepeatedField<global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics> resourceMetrics_ = new pbc::RepeatedField<global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics>();
    /// <summary>
    /// An array of ResourceMetrics.
    /// For data coming from a single resource this array will typically contain one
    /// element. Intermediary nodes (such as OpenTelemetry Collector) that receive
    /// data from multiple origins typically batch the data before forwarding further and
    /// in that case this array will contain multiple elements.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::RepeatedField<global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics> ResourceMetrics {
      get { return resourceMetrics_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ExportMetricsServiceRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ExportMetricsServiceRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if(!resourceMetrics_.Equals(other.resourceMetrics_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      hash ^= resourceMetrics_.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      resourceMetrics_.WriteTo(output, _repeated_resourceMetrics_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      resourceMetrics_.WriteTo(ref output, _repeated_resourceMetrics_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      size += resourceMetrics_.CalculateSize(_repeated_resourceMetrics_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ExportMetricsServiceRequest other) {
      if (other == null) {
        return;
      }
      resourceMetrics_.Add(other.resourceMetrics_);
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            resourceMetrics_.AddEntriesFrom(input, _repeated_resourceMetrics_codec);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            resourceMetrics_.AddEntriesFrom(ref input, _repeated_resourceMetrics_codec);
            break;
          }
        }
      }
    }
    #endif

  }

  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class ExportMetricsServiceResponse : pb::IMessage<ExportMetricsServiceResponse>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ExportMetricsServiceResponse> _parser = new pb::MessageParser<ExportMetricsServiceResponse>(() => new ExportMetricsServiceResponse());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ExportMetricsServiceResponse> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::OpenTelemetry.Proto.Collector.Metrics.V1.MetricsServiceReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceResponse() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceResponse(ExportMetricsServiceResponse other) : this() {
      partialSuccess_ = other.partialSuccess_ != null ? other.partialSuccess_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsServiceResponse Clone() {
      return new ExportMetricsServiceResponse(this);
    }

    /// <summary>Field number for the "partial_success" field.</summary>
    public const int PartialSuccessFieldNumber = 1;
    private global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess partialSuccess_;
    /// <summary>
    /// The details of a partially successful export request.
    ///
    /// If the request is only partially accepted
    /// (i.e. when the server accepts only parts of the data and rejects the rest)
    /// the server MUST initialize the `partial_success` field and MUST
    /// set the `rejected_&lt;signal>` with the number of items it rejected.
    ///
    /// Servers MAY also make use of the `partial_success` field to convey
    /// warnings/suggestions to senders even when the request was fully accepted.
    /// In such cases, the `rejected_&lt;signal>` MUST have a value of `0` and
    /// the `error_message` MUST be non-empty.
    ///
    /// A `partial_success` message with an empty value (rejected_&lt;signal> = 0 and
    /// `error_message` = "") is equivalent to it not being set/present. Senders
    /// SHOULD interpret it the same way as in the full success case.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess PartialSuccess {
      get { return partialSuccess_; }
      set {
        partialSuccess_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ExportMetricsServiceResponse);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ExportMetricsServiceResponse other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(PartialSuccess, other.PartialSuccess)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (partialSuccess_ != null) hash ^= PartialSuccess.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (partialSuccess_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(PartialSuccess);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (partialSuccess_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(PartialSuccess);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (partialSuccess_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(PartialSuccess);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ExportMetricsServiceResponse other) {
      if (other == null) {
        return;
      }
      if (other.partialSuccess_ != null) {
        if (partialSuccess_ == null) {
          PartialSuccess = new global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess();
        }
        PartialSuccess.MergeFrom(other.PartialSuccess);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (partialSuccess_ == null) {
              PartialSuccess = new global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess();
            }
            input.ReadMessage(PartialSuccess);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (partialSuccess_ == null) {
              PartialSuccess = new global::OpenTelemetry.Proto.Collector.Metrics.V1.ExportMetricsPartialSuccess();
            }
            input.ReadMessage(PartialSuccess);
            break;
          }
        }
      }
    }
    #endif

  }

  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class ExportMetricsPartialSuccess : pb::IMessage<ExportMetricsPartialSuccess>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ExportMetricsPartialSuccess> _parser = new pb::MessageParser<ExportMetricsPartialSuccess>(() => new ExportMetricsPartialSuccess());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<ExportMetricsPartialSuccess> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::OpenTelemetry.Proto.Collector.Metrics.V1.MetricsServiceReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsPartialSuccess() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsPartialSuccess(ExportMetricsPartialSuccess other) : this() {
      rejectedDataPoints_ = other.rejectedDataPoints_;
      errorMessage_ = other.errorMessage_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public ExportMetricsPartialSuccess Clone() {
      return new ExportMetricsPartialSuccess(this);
    }

    /// <summary>Field number for the "rejected_data_points" field.</summary>
    public const int RejectedDataPointsFieldNumber = 1;
    private long rejectedDataPoints_;
    /// <summary>
    /// The number of rejected data points.
    ///
    /// A `rejected_&lt;signal>` field holding a `0` value indicates that the
    /// request was fully accepted.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public long RejectedDataPoints {
      get { return rejectedDataPoints_; }
      set {
        rejectedDataPoints_ = value;
      }
    }

    /// <summary>Field number for the "error_message" field.</summary>
    public const int ErrorMessageFieldNumber = 2;
    private string errorMessage_ = "";
    /// <summary>
    /// A developer-facing human-readable message in English. It should be used
    /// either to explain why the server rejected parts of the data during a partial
    /// success or to convey warnings/suggestions during a full success. The message
    /// should offer guidance on how users can address such issues.
    ///
    /// error_message is an optional field. An error_message with an empty value
    /// is equivalent to it not being set.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string ErrorMessage {
      get { return errorMessage_; }
      set {
        errorMessage_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as ExportMetricsPartialSuccess);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(ExportMetricsPartialSuccess other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (RejectedDataPoints != other.RejectedDataPoints) return false;
      if (ErrorMessage != other.ErrorMessage) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (RejectedDataPoints != 0L) hash ^= RejectedDataPoints.GetHashCode();
      if (ErrorMessage.Length != 0) hash ^= ErrorMessage.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (RejectedDataPoints != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(RejectedDataPoints);
      }
      if (ErrorMessage.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(ErrorMessage);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (RejectedDataPoints != 0L) {
        output.WriteRawTag(8);
        output.WriteInt64(RejectedDataPoints);
      }
      if (ErrorMessage.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(ErrorMessage);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (RejectedDataPoints != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(RejectedDataPoints);
      }
      if (ErrorMessage.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(ErrorMessage);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(ExportMetricsPartialSuccess other) {
      if (other == null) {
        return;
      }
      if (other.RejectedDataPoints != 0L) {
        RejectedDataPoints = other.RejectedDataPoints;
      }
      if (other.ErrorMessage.Length != 0) {
        ErrorMessage = other.ErrorMessage;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            RejectedDataPoints = input.ReadInt64();
            break;
          }
          case 18: {
            ErrorMessage = input.ReadString();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            RejectedDataPoints = input.ReadInt64();
            break;
          }
          case 18: {
            ErrorMessage = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
