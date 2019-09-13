using Amazon;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Kitsune.Identifier.Constants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using static Kitsune.Identifier.Helpers.AWSHelper;

namespace Kitsune.Identifier.Helpers
{
	public class AWSHelper
	{
		public class AmazonS3Result
		{
			public bool IsSuccess { get; set; }
			public string Message { get; set; }
		}

		public class AmazonS3File
		{
			public byte[] Content { get; set; }
			public string ContentType { get; set; }
			public NameValueCollection Headers { get; set; }
		}

		public class AmazonS3GetAssetResult : AmazonS3Result
		{
			public AmazonS3File File { get; set; }
		}

		public class AWSS3Helper
		{
			public static NameValueCollection ToNameValueCollection(HeadersCollection headersCollection)
			{
				NameValueCollection collection = new NameValueCollection();
				if (headersCollection != null)
					foreach (var item in headersCollection.Keys)
					{
						collection.Add(item, headersCollection[item]);
					}
				return collection;
			}

			public static AmazonS3GetAssetResult GetAssetFromS3(string bucketName, string fileNameAndPath)
			{
				try
				{
					if (String.IsNullOrEmpty(fileNameAndPath))
						throw new ArgumentNullException(nameof(fileNameAndPath));
					if (String.IsNullOrEmpty(bucketName))
						throw new ArgumentNullException(nameof(bucketName));

					var client = new AmazonS3Client(new AmazonS3Config()
					{
						RegionEndpoint = RegionEndpoint.APSouth1,
						UseAccelerateEndpoint = true
					});

					GetObjectRequest request = new GetObjectRequest
					{
						BucketName = bucketName,
						Key = fileNameAndPath
					};
					GetObjectResponse response = client.GetObjectAsync(request).Result;

					if (response.HttpStatusCode.Equals(HttpStatusCode.OK))
					{
						Stream responseStream = response.ResponseStream;
						var byteData = StreamHelper.ReadFully(responseStream);
						AmazonS3File file = new AmazonS3File
						{
							Headers = AWSS3Helper.ToNameValueCollection(response.Headers),
							Content = byteData,
							ContentType = response.Headers.ContentType
						};
						return new AmazonS3GetAssetResult { IsSuccess = true, File = file };
					}
					else
					{
						return new AmazonS3GetAssetResult { IsSuccess = false, Message = $"Error getting the file, statuscode : {response.HttpStatusCode} and AWSRequestId : {response.ResponseMetadata?.RequestId}" };
					}
				}
				catch (Exception ex)
				{
					return new AmazonS3GetAssetResult { IsSuccess = false, Message = $"Error getting file from s3" };
				}
			}

		}

		public class RoutingLambdaRequestModel
		{
			public string body;
		}


	}
	public class AWSLambdaHelpers
	{
		public static async Task<string> InvokeAWSLambda(string functionName, string payLoad, RegionEndpoint regionEndpoint)
		{
			try
			{
				if (!String.IsNullOrEmpty(functionName) && !String.IsNullOrEmpty(payLoad))
				{
					var newPayload = new RoutingLambdaRequestModel() { body = payLoad };
					var lambdaClient = new AmazonLambdaClient(IdentifierEnvironmentConstants.IdentifierConfigurations.RoutingLambdaCredentials.AccessKey, IdentifierEnvironmentConstants.IdentifierConfigurations.RoutingLambdaCredentials.Secret, regionEndpoint);

					var response = await lambdaClient.InvokeAsync(new InvokeRequest
					{
						FunctionName = functionName,
						InvocationType = InvocationType.RequestResponse,
						LogType = LogType.None,
						Payload = JsonConvert.SerializeObject(newPayload)
					});
					
					using (var reader = response.Payload)
					{
						return new StreamReader(reader).ReadToEnd();
					}

				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return null;
		}
	}
}
