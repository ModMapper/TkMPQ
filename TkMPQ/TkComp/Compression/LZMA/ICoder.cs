// ICoder.h

using System.IO;
using System;

namespace SevenZip
{
	class DataErrorException : IOException
	{
		public DataErrorException(): base("Data Error") { }
	}
    
	class InvalidParamException : ArgumentException
	{
		public InvalidParamException(): base("Invalid Parameter") { }
	}

	internal interface ICodeProgress
	{
		void SetProgress(Int64 inSize, Int64 outSize);
	};

    internal interface ICoder
	{
		void Code(System.IO.Stream inStream, System.IO.Stream outStream,
			Int64 inSize, Int64 outSize, ICodeProgress progress);
	};

    /*
	internal interface ICoder2
	{
		 void Code(ISequentialInStream []inStreams,
				const UInt64 []inSizes, 
				ISequentialOutStream []outStreams, 
				UInt64 []outSizes,
				ICodeProgress progress);
	};
  */
  
    internal enum CoderPropID
	{
		DefaultProp = 0,
		DictionarySize,
		UsedMemorySize,
		Order,
		BlockSize,
		PosStateBits,
		LitContextBits,
		LitPosBits,
		NumFastBytes,
		MatchFinder,
		MatchFinderCycles,
		NumPasses,
		Algorithm,
		NumThreads,
		EndMarker
	};


    internal interface ISetCoderProperties
	{
		void SetCoderProperties(CoderPropID[] propIDs, object[] properties);
	};

    internal interface IWriteCoderProperties
	{
		void WriteCoderProperties(System.IO.Stream outStream);
	}

    internal interface ISetDecoderProperties
	{
		void SetDecoderProperties(byte[] properties);
	}
}
