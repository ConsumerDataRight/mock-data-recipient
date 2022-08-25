using CDR.DataRecipient.IntegrationTests.Fixtures;
using CDR.DataRecipient.IntegrationTests.Infrastructure;
using CDR.DataRecipient.IntegrationTests.Infrastructure.API2;
using CDR.DataRecipient.SDK.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.IntegrationTests
{
    public class US12693_MDR_HostArrangementRevocation_JWT : BaseTest
    {
        private class ConsentArrangement
        {
            public string? DataHolderBrandId { get; init; }
            public string? ClientId { get; init; }
            public int? SharingDuration { get; init; }
            public string? CdrArrangementId { get; init; }
            public string? IdToken { get; init; }
            public string? AccessToken { get; init; }
            public string? RefreshToken { get; init; }
            public int? ExpiresIn { get; init; }
            public string? Scope { get; init; }
            public string? TokenType { get; init; }
            public DateTime? CreatedOn { get; init; }
        }

        private static async Task<string> Arrange(string? dataHolderBrandId = null)
        {
            TestSetup.PatchRegister();

            // Get access token for Register
            async Task<string> GetAccessTokenFromRegister()
            {
                var registerAccessToken = await new Infrastructure.AccessToken
                {
                    URL = REGISTER_MTLS_TOKEN_URL,
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    JWT_CertificateFilename = JWT_CERTIFICATE_FILENAME,
                    JWT_CertificatePassword = JWT_CERTIFICATE_PASSWORD,
                    ClientId = SOFTWAREPRODUCT_ID,
                    Scope = "cdr-register:bank:read cdr-register:read",
                    ClientAssertionType = CLIENTASSERTIONTYPE,
                    GrantType = "client_credentials",
                    Issuer = SOFTWAREPRODUCT_ID,
                    Audience = REGISTER_MTLS_TOKEN_URL
                }.GetAsync();

                if (registerAccessToken == null)
                {
                    throw new Exception("Error getting register access token");
                }

                return registerAccessToken;
            }

            // Get data holder brands from register
            async Task<List<DataHolderBrand>> GetDataHolderBrandsFromRegister(string token)
            {
                var apiCall = new Infrastructure.API
                {
                    CertificateFilename = CERTIFICATE_FILENAME,
                    CertificatePassword = CERTIFICATE_PASSWORD,
                    HttpMethod = HttpMethod.Get,
                    URL = REGISTER_MTLS_DATAHOLDERBRANDS_URL,
                    AccessToken = token,
                    XV = "2"
                };

                var response = await apiCall.SendAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"{response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }

                var json = await response.Content.ReadAsStringAsync();

                var brandList = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<List<DataHolderBrand>>>(json)?.Data;

                return brandList ?? throw new Exception();
            }

            // Save data holder brands
            void PersistDataHolderBrands(List<DataHolderBrand> brandList)
            {
                // Connect
                using var connection = new SqlConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
                connection.Open();

                // Purge table
                using var deleteCommand = new SqlCommand($"delete from dataholderbrand", connection);
                deleteCommand.ExecuteNonQuery();

                // Save each brand
                foreach (var brand in brandList)
                {
                    var jsonDocument = System.Text.Json.JsonSerializer.Serialize(brand);

                    using var insertCommand = new SqlCommand($"insert into dataholderbrand (dataholderbrandid, jsondocument) values (@dataholderbrandid, @jsondocument)", connection);
                    insertCommand.Parameters.AddWithValue("@dataholderbrandid", brand.DataHolderBrandId);
                    insertCommand.Parameters.AddWithValue("@jsondocument", jsonDocument);
                    insertCommand.ExecuteNonQuery();
                }

                // Check count
                using var selectCommand = new SqlCommand($"select count(*) from dataholderbrand", connection);
                var count = Convert.ToInt32(selectCommand.ExecuteScalar());
                if (count != brandList.Count)
                {
                    throw new Exception($"{nameof(PersistDataHolderBrands)} - Error persisting brands");
                }
            }

            // Create and save a consent arrangement
            string CreateConsentArrangement(string dataHolderBrandId, string clientId)
            {
                var consentArrangement = new ConsentArrangement
                {
                    CdrArrangementId = Guid.NewGuid().ToString(),
                    DataHolderBrandId = dataHolderBrandId,
                    ClientId = clientId,
                    ExpiresIn = 300,
                    CreatedOn = DateTime.Now,
                    SharingDuration = 0,
                };

                // Connect
                using var connection = new SqlConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
                connection.Open();

                // Purge table
                using var deleteCommand = new SqlCommand($"delete from cdrarrangement", connection);
                deleteCommand.ExecuteNonQuery();

                // Save arrangement
                var jsonDocument = System.Text.Json.JsonSerializer.Serialize(consentArrangement);
                using var insertCommand = new SqlCommand($"insert into cdrarrangement (cdrarrangementid, clientid, jsondocument) values (@cdrarrangementid, @clientid, @jsondocument)", connection);
                insertCommand.Parameters.AddWithValue("@cdrarrangementid", consentArrangement.CdrArrangementId);
                insertCommand.Parameters.AddWithValue("@clientid", consentArrangement.ClientId);
                insertCommand.Parameters.AddWithValue("@jsondocument", jsonDocument);
                insertCommand.ExecuteNonQuery();

                using var selectCommand = new SqlCommand($"select count(*) from cdrarrangement", connection);
                var count = Convert.ToInt32(selectCommand.ExecuteScalar());
                if (count != 1)
                {
                    throw new Exception($"{nameof(CreateConsentArrangement)} - Error creating consent arrangement");
                }

                return consentArrangement.CdrArrangementId;
            }

            var registerToken = await GetAccessTokenFromRegister();
            var brandsList = await GetDataHolderBrandsFromRegister(registerToken);
            PersistDataHolderBrands(brandsList);
            return CreateConsentArrangement(dataHolderBrandId ?? DATAHOLDER_BRAND.ToLower(), SOFTWAREPRODUCT_ID);
        }

        private static string GetClientAssertion()
        {
            return new ClientAssertion
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                Iss = DATAHOLDER_BRAND.ToLower(),
                Aud = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                Kid = "7C5716553E9B132EF325C49CA2079737196C03DB",  // Kid for this dataholder
            }.Get();
        }

        private static string CreateCdrArrangementJwt(
            string? cdrArrangementId = null,
            string? iss = null,
            string? sub = null,
            string? aud = null,
            string? jti = null,
            int? expiryInSeconds = null,
            string? kid = null,
            string signingCertificateFileName = DATAHOLDER_CERTIFICATE_FILENAME,
            string signingCertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD)
        {
            var claims = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(cdrArrangementId))
                claims.Add("cdr_arrangement_id", cdrArrangementId);

            if (!string.IsNullOrEmpty(iss))
                claims.Add("iss", iss.ToLower());

            if (!string.IsNullOrEmpty(sub))
                claims.Add("sub", sub.ToLower());

            if (!string.IsNullOrEmpty(aud))
                claims.Add("aud", aud);

            if (!string.IsNullOrEmpty(jti))
                claims.Add("jti", jti);

            if (expiryInSeconds.HasValue)
                claims.Add("exp", (DateTime.UtcNow.AddSeconds(expiryInSeconds.Value) - new DateTime(1970, 1, 1)).TotalSeconds);

            return JWT2.CreateJWT(
                signingCertificateFileName,
                signingCertificatePassword,
                claims,
                kid ?? "7C5716553E9B132EF325C49CA2079737196C03DB");
        }

        [Fact]
        public async Task AC01_Post_WithCDRArrangementIdParameterOnly_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_id", "123")
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Missing"",
                            ""title"": ""Missing Required Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC02_Post_WithEmptyCDRArrangementJwtParameter_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", "")
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Missing"",
                            ""title"": ""Missing Required Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC03_Post_WithNullCDRArrangementJwtParameter_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", null)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Missing"",
                            ""title"": ""Missing Required Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC03b_Post_WithEmptyCDRArrangementJWTPayload_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt();
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                    ""errors"": [{
                        ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                        ""title"": ""Invalid Field"",
                        ""detail"": ""cdr_arrangement_jwt""
                    }]
                }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC04_Post_WithCDRArrangementJwtWithNoCdrArrangementId_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(null, DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC04b_Post_WithCDRArrangementJwtWithEmptyCdrArrangementId_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt("", DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC05_Post_WithNonMatchingCDRArrangementIdValues_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt("1", DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt),
                        new KeyValuePair<string?, string?>("cdr_arrangement_id", "2")
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_id""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC06_Post_WithInvalidCDRArrangementIdValue_ShouldRespondWith_422UnprocessableEntity_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt("foo", DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                            ""title"": ""Invalid Consent Arrangement"",
                            ""detail"": ""foo""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC07_Post_WithUnownedCDRArrangementId_ShouldRespondWith_422UnprocessableEntity_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange(Guid.NewGuid().ToString());
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",
                            ""title"": ""Invalid Consent Arrangement"",
                            ""detail"": ""#{cdrArrangementId}"" 
                        }]
                    }".Replace("#{cdrArrangementId}", cdrArrangementId);
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        // TODO: this is an interesting case.  Having a non-matching kid (key id) does not break the validation.
        // It appears like the MS library will validate the signature anyway with the key values in the JWKS even if the kid doesn't directly match.
        //[Fact]
        //public async Task AC08_Post_WithInvalidSignatureCDRArrangementJwt_ShouldRespondWith_400InvalidField_ErrorResponse()
        //{
        //    // Arrange 
        //    var cdrArrangementId = await Arrange();
        //    var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, kid: "foo");
        //    var apiCall = new Infrastructure.API
        //    {
        //        CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
        //        CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
        //        HttpMethod = HttpMethod.Post,
        //        URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
        //        AccessToken = GetClientAssertion(),
        //        Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
        //            {
        //                new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
        //            }),
        //        ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
        //    };

        //    // Act
        //    var response = await apiCall.SendAsync();

        //    // Assert
        //    using (new AssertionScope())
        //    {
        //        // Assert - Check status code
        //        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        //        var expectedContent = @"{
        //                ""errors"": [{
        //                    ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
        //                    ""title"": ""Invalid Field"",
        //                    ""detail"": ""cdr_arrangement_jwt""
        //                }]
        //            }";
        //        await Assert_HasContent_Json(expectedContent, response.Content);
        //    }
        //}

        [Fact]
        public async Task AC08b_Post_WithInvalidSignatureCDRArrangementJwt_ShouldRespondWith_400InvalidField_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, signingCertificateFileName: JWT_CERTIFICATE_FILENAME, signingCertificatePassword: JWT_CERTIFICATE_PASSWORD);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC09_Post_WithInvalidContentTypeHeader_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt("123");
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/json"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Header/Invalid"",
                            ""title"": ""Invalid Header"",
                            ""detail"": """"
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC10_Post_WithInvalidClientAssertion_ShouldRespondWith_401Unauthorized_ErrorResponse()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = "foo" ?? GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

                // Assert - Check error response 
                Assert_HasHeader(@"Bearer error=""invalid_token""",
                    response.Headers,
                    "WWW-Authenticate"
                    ); // true); // starts with
            }
        }

        [Fact]
        public async Task AC11_Post_WithMinimalCDRArrangementJWT_ShouldRespondWith_204NoContent()
        {
            // Arrange 
            var clientAssertion = GetClientAssertion();
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = clientAssertion,
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }
        }

        //
        // TODO: The tests below should kick in from 15/11/2022
        //
        [Fact]
        public async Task AC12_Post_WithInvalidClaimsValuesInJWT_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (DateTime.UtcNow < AttemptValidateCdrArrangementJwtFromDate())
            {
                Assert.True(true, $"This test will not run until {AttemptValidateCdrArrangementJwtFromDate()}");
                return;
            }

            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, "1", "1", "https://foo.com", "123", 0);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC13_Post_WithUnmatchingIssAndSubInJWT_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (DateTime.UtcNow < AttemptValidateCdrArrangementJwtFromDate())
            {
                Assert.True(true, $"This test will not run until {AttemptValidateCdrArrangementJwtFromDate()}");
                return;
            }

            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, "foo1", "foo2", "https://foo.com", "123", 0);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC14_Post_WithExpiredJWT_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (DateTime.UtcNow < AttemptValidateCdrArrangementJwtFromDate())
            {
                Assert.True(true, $"This test will not run until {AttemptValidateCdrArrangementJwtFromDate()}");
                return;
            }

            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), -300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC15_Post_WithInvalidAudienceInJWT_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (DateTime.UtcNow < AttemptValidateCdrArrangementJwtFromDate())
            {
                Assert.True(true, $"This test will not run until {AttemptValidateCdrArrangementJwtFromDate()}");
                return;
            }

            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), "https://foo.com", Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC16_Post_WithInvalidIssuerInJWT_ShouldRespondWith_400BadRequest_ErrorResponse()
        {
            if (DateTime.UtcNow < AttemptValidateCdrArrangementJwtFromDate())
            {
                Assert.True(true, $"This test will not run until {AttemptValidateCdrArrangementJwtFromDate()}");
                return;
            }

            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, "foo", "foo", DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

                var expectedContent = @"{
                        ""errors"": [{
                            ""code"": ""urn:au-cds:error:cds-all:Field/Invalid"",
                            ""title"": ""Invalid Field"",
                            ""detail"": ""cdr_arrangement_jwt""
                        }]
                    }";
                await Assert_HasContent_Json(expectedContent, response.Content);
            }
        }

        [Fact]
        public async Task AC17_Post_WithFullJwt_ShouldRespondWith_204NoContent()
        {
            // Arrange 
            var cdrArrangementId = await Arrange();
            var cdrArrangementJwt = CreateCdrArrangementJwt(cdrArrangementId, DATAHOLDER_BRAND.ToLower(), DATAHOLDER_BRAND.ToLower(), DATARECIPIENT_ARRANGEMENTS_REVOKE_URL, Guid.NewGuid().ToString(), 300);
            var apiCall = new Infrastructure.API
            {
                CertificateFilename = DATAHOLDER_CERTIFICATE_FILENAME,
                CertificatePassword = DATAHOLDER_CERTIFICATE_PASSWORD,
                HttpMethod = HttpMethod.Post,
                URL = DATARECIPIENT_ARRANGEMENTS_REVOKE_URL,
                AccessToken = GetClientAssertion(),
                Content = new FormUrlEncodedContent(new List<KeyValuePair<string?, string?>>
                    {
                        new KeyValuePair<string?, string?>("cdr_arrangement_jwt", cdrArrangementJwt)
                    }),
                ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded"),
            };

            // Act
            var response = await apiCall.SendAsync();

            // Assert
            using (new AssertionScope())
            {
                // Assert - Check status code
                response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            }
        }

        public static DateTime AttemptValidateCdrArrangementJwtFromDate()
        {
            var obligationDate = Configuration["AttemptValidateCdrArrangementJwtFromDate"];
            if (string.IsNullOrEmpty(obligationDate))
            {
                return new DateTime(2022, 11, 15, 0, 0, 0, DateTimeKind.Utc);
            }

            return DateTime.Parse(obligationDate);
        }

    }
}
