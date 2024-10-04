import React, {useEffect, useState} from 'react';
import InnerHTML from 'dangerously-set-html-content';
import {
  Field,
  GetServerSideComponentProps,
  withDatasourceCheck
} from '@sitecore-jss/sitecore-jss-nextjs';
import { ComponentProps } from 'lib/component-props';

interface Fields {
  SessionizeUrl: Field<string>;
}

type AgendaProps = ComponentProps & {
  fields: Fields;
  data?: string;
  error?: boolean;
};

const AgendaDefaultComponent = (props: AgendaProps): JSX.Element => (
    <div className={`component promo ${props.params.styles}`}>
      <div className="component-content">
        <span className="is-empty-hint">Agenda</span>
      </div>
    </div>
);

const AgendaComponent = (props: AgendaProps): JSX.Element => {
  const [data, setData] = useState(props.data);
  const [error, setError] = useState(props.error);

  useEffect(() => {    
    if (!data) {
      // Fetch data on the client side
      const fetchData = async () => {
        try {
          const api = props.fields.SessionizeUrl.value;
          if (!api) {
            setError(true);
            return;
          }
          const res = await fetch(api);
          if (!res.ok) {
            throw new Error(`Failed to fetch data: ${res.statusText}`);
          }
          const fetchedData = await res.text();
          setData(fetchedData);
        } catch (err) {
          console.error('Error fetching data on client:', err);
          setError(true);
        }
      };
      fetchData();
    }
  }, []);
  
  if (error) {
    return <div>Failed to load...</div>;
  }

  if (!data) {
    return <div>Loading...</div>;
  }
  
  // Check if the data is valid HTML
  return (
      <div>
        <div dangerouslySetInnerHTML={{ __html: data }}></div> {/* Render it */}
      </div>
  );
};

export const getServerSideProps: GetServerSideComponentProps = async (rendering, layoutData, context) => {
  const isClientNavigation = context.req.url?.startsWith('/_next/data/');
  const api = rendering?.fields?.SessionizeUrl?.value;

  // If no API URL is provided, return empty props
  if (!api) {
    return {
    };
  }

  if (isClientNavigation) {
    // Skip server-side data fetching during client-side navigation
    return {
      props: {}
    };
  }

  try {
    // Fetch the data from the API
    const res = await fetch(api);
    if (!res.ok) {
      throw new Error(`Failed to fetch data: ${res.statusText}`);
    }
    const data = await res.text();

    // Return the data as props
    return {
      data
    };
  } catch (error) {
    // console.error('Error fetching data:', error);

    // Return an error prop to handle in the component
    return {
      error: true
    };
  }
};

export const Default = withDatasourceCheck()<AgendaProps>(AgendaComponent);
