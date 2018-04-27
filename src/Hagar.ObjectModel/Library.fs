namespace Hagar.ObjectModel
(*
open Hagar.Codecs
open Hagar.Utilities
open Hagar.WireProtocol
*)
module ObjectModel =
  type VarInt = byte[]

  type SchemaType =
    | Expected
    | WellKnown of VarInt
    | Encoded of byte[]
    | Referenced of VarInt

  type DataField = {FieldIdDelta:uint32; SchemaType:SchemaType}

  type FieldWithId =
    | VarInt of DataField * byte[]
    | StartObject of SchemaType
    | LengthPrefixed of VarInt * byte[]
    | Fixed32 of uint32
    | Fixed64 of uint64
    | Fixed128 of uint64 * uint64

  type ControlField = 
    | EndObject
    | EndBaseFields
  
  type Field =
    | FieldWithId
    | ControlField

(*
  let Parse (reader : Hagar.Buffers.Reader) (session : Hagar.Session.SerializerSession) =
    let field = FieldHeaderCodec.ReadFieldHeader(reader, session)
    field
*)
 